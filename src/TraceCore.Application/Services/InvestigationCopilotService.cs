using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;

namespace TraceCore.Application.Services;

/// <summary>
/// Prompt 3 — Copiloto de investigação baseado no histórico corporativo.
/// Ordem: histórico interno (mesmo cliente+produto → produto → componente/versão →
/// erro exato) → conhecimento validado → contexto técnico → hipótese da IA.
/// Nunca exige embedding — RAG semântico (<see cref="IRagService"/>) permanece
/// disponível separadamente como enriquecimento, não como caminho obrigatório.
/// Registrado como Scoped: os campos de estado por pergunta (fontes usadas,
/// estratégias, contadores) vivem na instância durante uma única chamada a AskAsync.
/// </summary>
public class InvestigationCopilotService : IInvestigationCopilotService
{
    private const int MaxToolCalls = 8;
    private const int MaxCaseDetailCalls = 5;
    private const int MaxSearchResultsPerCall = 20;
    private const int MaxExternalSearchCalls = 3;

    private readonly ILlmProviderResolver _resolver;
    private readonly ISearchRepository _searchRepository;
    private readonly IKnowledgeService _knowledgeService;
    private readonly ICaseService _caseService;
    private readonly ICaseResolutionService _caseResolutionService;
    private readonly ICaseRelationService _caseRelationService;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IProductTechnicalContextService _technicalContextService;
    private readonly IExternalResearchService _externalResearchService;
    private readonly IAiInteractionRepository _aiInteractionRepository;

    // Estado por pergunta (instância Scoped — uma AskAsync por request).
    private readonly HashSet<string> _strategies = new();
    private readonly Dictionary<long, RetrievedCaseDto> _relatedCases = new();
    private readonly Dictionary<long, RetrievedKnowledgeDto> _relatedKnowledge = new();
    private readonly Dictionary<string, RetrievedExternalSourceDto> _externalSourcesUsed = new(); // key: Url
    private bool _usedTechnicalContext;
    private int _caseDetailCallsUsed;
    private int _externalSearchCallsUsed;

    public InvestigationCopilotService(
        ILlmProviderResolver resolver,
        ISearchRepository searchRepository,
        IKnowledgeService knowledgeService,
        ICaseService caseService,
        ICaseResolutionService caseResolutionService,
        ICaseRelationService caseRelationService,
        ICatalogRepository catalogRepository,
        IClientRepository clientRepository,
        IProductTechnicalContextService technicalContextService,
        IExternalResearchService externalResearchService,
        IAiInteractionRepository aiInteractionRepository)
    {
        _resolver = resolver;
        _searchRepository = searchRepository;
        _knowledgeService = knowledgeService;
        _caseService = caseService;
        _caseResolutionService = caseResolutionService;
        _caseRelationService = caseRelationService;
        _catalogRepository = catalogRepository;
        _clientRepository = clientRepository;
        _technicalContextService = technicalContextService;
        _externalResearchService = externalResearchService;
        _aiInteractionRepository = aiInteractionRepository;
    }

    public async Task<InvestigationCopilotAnswerDto> AskAsync(string question, long? userId, CancellationToken ct = default)
    {
        _strategies.Clear();
        _relatedCases.Clear();
        _relatedKnowledge.Clear();
        _externalSourcesUsed.Clear();
        _usedTechnicalContext = false;
        _caseDetailCallsUsed = 0;
        _externalSearchCallsUsed = 0;

        var sw = Stopwatch.StartNew();
        question = question?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(question))
            throw new BusinessRuleValidationException("BR-080", "A pergunta do copiloto é obrigatória.");

        // Fail-fast só para o provedor de GERAÇÃO — embedding nunca é resolvido aqui.
        var generationProvider = await _resolver.ResolveGenerationProviderAsync(ct);

        var warnings = new List<string>();

        // 1. Interpretação (sem ferramentas) — extrai sinais do relato.
        var intent = await InterpretAsync(question, generationProvider, ct);

        // 2. Resolução de entidades no backend — o LLM nunca fornece IDs (§8).
        var client = await ResolveClientAsync(intent.ClientText, ct);
        var product = await ResolveProductAsync(intent.ProductText, ct);

        if (client.Ambiguous)
            warnings.Add($"Cliente ambíguo a partir do relato ('{intent.ClientText}'): {string.Join(", ", client.Candidates)}. Refine a pergunta se necessário.");
        if (product.Ambiguous)
            warnings.Add($"Produto ambíguo a partir do relato ('{intent.ProductText}'): {string.Join(", ", product.Candidates)}. Refine a pergunta se necessário.");

        // 3. Loop de tool-calling multi-turno (até MaxToolCalls).
        var systemPrompt = BuildSystemPrompt(client, product, intent);
        var turns = new List<LlmConversationTurn>();
        string finalText = string.Empty;
        int toolCallCount = 0;
        bool naturalStop = false;

        for (int round = 0; round < MaxToolCalls && toolCallCount < MaxToolCalls; round++)
        {
            var result = await generationProvider.GenerateAsync(new LlmGenerationRequest(
                SystemPrompt: systemPrompt,
                UserPrompt: question,
                Tools: AiToolDefinitions.ReadingTools,
                PriorTurns: turns), ct);

            if (result.ToolCalls == null || result.ToolCalls.Count == 0)
            {
                finalText = result.Text;
                naturalStop = true;
                break;
            }

            turns.Add(new LlmConversationTurn(Role: "assistant", Text: result.Text, ToolCalls: result.ToolCalls));

            foreach (var call in result.ToolCalls)
            {
                toolCallCount++;
                var (resultJson, isError) = await ExecuteToolAsync(call, ct);
                turns.Add(new LlmConversationTurn(Role: "tool", ToolCallId: call.Id, ToolResultJson: resultJson, ToolResultIsError: isError));

                if (toolCallCount >= MaxToolCalls) break;
            }

            finalText = result.Text;
        }

        if (!naturalStop)
        {
            warnings.Add("Limite de chamadas de ferramentas atingido nesta investigação.");
            // Força uma síntese final sem ferramentas, para não devolver resposta vazia/truncada.
            var finalResult = await generationProvider.GenerateAsync(new LlmGenerationRequest(
                SystemPrompt: systemPrompt,
                UserPrompt: question,
                Tools: null,
                PriorTurns: turns), ct);
            finalText = finalResult.Text;
        }

        sw.Stop();

        if (_relatedCases.Count == 0 && _relatedKnowledge.Count == 0 && !_usedTechnicalContext)
        {
            warnings.Add("Não foi encontrado histórico interno suficientemente relacionado — a resposta pode conter apenas hipóteses da IA.");
        }

        var metadata = JsonSerializer.Serialize(new
        {
            RetrievalStrategies = _strategies.ToList(),
            ToolCallCount = toolCallCount,
            RelatedCasesCount = _relatedCases.Count,
            RelatedKnowledgeCount = _relatedKnowledge.Count,
            UsedTechnicalContext = _usedTechnicalContext,
            SemanticSearchUsed = false,
            ExternalResearchUsed = _externalSourcesUsed.Count > 0,
            ExternalResearchCalls = _externalSearchCallsUsed,
            ExternalSourcesCount = _externalSourcesUsed.Count
        });

        var interaction = new AiInteraction(
            userId: userId,
            queryText: question,
            responseText: finalText,
            providerCode: generationProvider.ProviderCode,
            modelName: generationProvider.ModelName,
            tokensUsed: null,
            latencyMs: sw.ElapsedMilliseconds,
            metadataJson: metadata);

        var interactionId = await _aiInteractionRepository.AddInteractionAsync(interaction, ct);

        var sources = new List<AiSource>();
        int rank = 1;
        foreach (var c in _relatedCases.Values)
        {
            sources.Add(AiSource.ForStructuredSource(interactionId, "HistoricalCase", c.CaseId, rank++, c.MatchScore));
        }
        foreach (var k in _relatedKnowledge.Values)
        {
            sources.Add(AiSource.ForStructuredSource(interactionId, "ValidatedKnowledge", k.KnowledgeItemId, rank++));
        }
        foreach (var ext in _externalSourcesUsed.Values)
        {
            sources.Add(AiSource.ForExternalSource(interactionId, ext.Url, ext.Title, rank++));
        }
        if (sources.Count > 0)
        {
            await _aiInteractionRepository.AddSourcesAsync(sources, ct);
        }

        return new InvestigationCopilotAnswerDto(
            InteractionId: interactionId,
            Question: question,
            Answer: finalText,
            RelatedCases: _relatedCases.Values.ToList(),
            RelatedKnowledge: _relatedKnowledge.Values.ToList(),
            ExternalSources: _externalSourcesUsed.Values.ToList(),
            UsedTechnicalContext: _usedTechnicalContext,
            RetrievalStrategies: _strategies.ToList(),
            Warnings: warnings,
            ToolCallCount: toolCallCount,
            ProviderCode: generationProvider.ProviderCode,
            ModelName: generationProvider.ModelName,
            LatencyMs: sw.ElapsedMilliseconds);
    }

    private static async Task<InvestigationIntent> InterpretAsync(string question, ILlmProvider provider, CancellationToken ct)
    {
        const string interpretationPrompt = """
            Extraia sinais estruturados do relato abaixo. Responda APENAS com um objeto JSON,
            sem texto adicional, no formato exato:
            {"clientText": string ou null, "productText": string ou null, "symptoms": [string],
             "errorCodes": [string], "componentHint": string ou null, "versionHint": string ou null,
             "environmentHint": string ou null}
            Não invente informação que não esteja no relato. Campos não identificados devem ser null
            ou lista vazia.
            """;

        try
        {
            var result = await provider.GenerateAsync(new LlmGenerationRequest(
                SystemPrompt: interpretationPrompt,
                UserPrompt: question,
                MaxTokens: 1024), ct);

            var jsonText = ExtractJsonObject(result.Text);
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            string? Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            List<string> Arr(string name)
            {
                if (root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array)
                    return v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToList();
                return new List<string>();
            }

            return new InvestigationIntent(
                ClientText: Str("clientText"),
                ProductText: Str("productText"),
                Symptoms: Arr("symptoms"),
                ErrorCodes: Arr("errorCodes"),
                ComponentHint: Str("componentHint"),
                VersionHint: Str("versionHint"),
                EnvironmentHint: Str("environmentHint"));
        }
        catch (Exception)
        {
            // Interpretação é um auxílio, não um requisito — se falhar (JSON malformado,
            // etc.), a investigação continua com sinais vazios em vez de quebrar tudo.
            return new InvestigationIntent(null, null, Array.Empty<string>(), Array.Empty<string>(), null, null, null);
        }
    }

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start) return text[start..(end + 1)];
        return text;
    }

    private async Task<ResolvedEntity> ResolveClientAsync(string? text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return new ResolvedEntity(null, null, false, Array.Empty<string>());

        var clients = await _clientRepository.GetAllAsync(ct);
        var exact = clients.FirstOrDefault(c => string.Equals(c.Name, text, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return new ResolvedEntity(exact.Id, exact.Name, false, Array.Empty<string>());

        var partial = clients.Where(c => c.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
        if (partial.Count == 1) return new ResolvedEntity(partial[0].Id, partial[0].Name, false, Array.Empty<string>());
        if (partial.Count > 1) return new ResolvedEntity(null, null, true, partial.Select(c => c.Name).ToList());

        return new ResolvedEntity(null, null, false, Array.Empty<string>());
    }

    private async Task<ResolvedEntity> ResolveProductAsync(string? text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return new ResolvedEntity(null, null, false, Array.Empty<string>());

        var products = await _catalogRepository.GetAllProductsAsync(ct);
        var exact = products.FirstOrDefault(p => string.Equals(p.Name, text, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return new ResolvedEntity(exact.Id, exact.Name, false, Array.Empty<string>());

        var partial = products.Where(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
        if (partial.Count == 1) return new ResolvedEntity(partial[0].Id, partial[0].Name, false, Array.Empty<string>());
        if (partial.Count > 1) return new ResolvedEntity(null, null, true, partial.Select(p => p.Name).ToList());

        return new ResolvedEntity(null, null, false, Array.Empty<string>());
    }

    private static string BuildSystemPrompt(ResolvedEntity client, ResolvedEntity product, InvestigationIntent intent)
    {
        string ClientLine()
        {
            if (client.Ambiguous) return $"Cliente citado no relato é AMBÍGUO (candidatos: {string.Join(", ", client.Candidates)}) — não escolha um sozinho, mencione a ambiguidade se relevante.";
            if (client.Id.HasValue) return $"Cliente identificado: {client.Name} (ClientId={client.Id.Value}).";
            return "Cliente não identificado no relato — não filtre por cliente.";
        }

        string ProductLine()
        {
            if (product.Ambiguous) return $"Produto citado no relato é AMBÍGUO (candidatos: {string.Join(", ", product.Candidates)}) — não escolha um sozinho, mencione a ambiguidade se relevante.";
            if (product.Id.HasValue) return $"Produto identificado: {product.Name} (ProductId={product.Id.Value}).";
            return "Produto não identificado no relato — não filtre por produto até identificá-lo por outra evidência.";
        }

        return $"""
            Você é o assistente investigativo do TraceCore — plataforma corporativa de conhecimento,
            troubleshooting e lições aprendidas.

            {ClientLine()}
            {ProductLine()}
            Sintomas extraídos do relato: {(intent.Symptoms.Count > 0 ? string.Join(", ", intent.Symptoms) : "nenhum sintoma específico extraído")}.
            Códigos de erro extraídos: {(intent.ErrorCodes.Count > 0 ? string.Join(", ", intent.ErrorCodes) : "nenhum")}.

            Antes de propor causas ou soluções:
            1. Pesquise o histórico interno quando houver contexto suficiente — comece por
               SearchCases com cliente+produto (quando ambos identificados), amplie progressivamente
               para produto em outros clientes, depois componente/versão/ambiente, depois erro exato.
            2. Não se limite a casos Resolved — casos em andamento (Open/Investigating) também são
               relevantes e devem ser citados como tal, nunca como solução validada.
            3. Procure conhecimento validado com SearchKnowledge/GetKnowledgeDetails.
            4. Use GetProductContext quando o histórico não for suficiente e for necessário raciocinar
               sobre a arquitetura da aplicação — nunca afirme como fato tecnologia/dependência que não
               apareça no resultado da ferramenta.
            5. Use GetRelatedCases para expandir a partir de um caso já identificado como relevante.
            6. Só depois de esgotar histórico interno, conhecimento validado e contexto técnico — e só
               quando isso agregar valor real (erro desconhecido, tecnologia externa, versão recente) —
               use SearchExternalSources. A política de pesquisa externa do produto é decidida pelo
               backend, não por você; se a ferramenta responder "rejected", aceite e siga sem pesquisa
               externa, sem tratar isso como erro.
            7. Diferencie sempre fato de hipótese. Rotule explicitamente: HISTÓRICO INTERNO,
               SOLUÇÃO VALIDADA, CONTEXTO TÉCNICO, DOCUMENTAÇÃO EXTERNA, HIPÓTESE DA IA. Nunca apresente
               fonte externa como conhecimento interno da empresa — se documentação externa conflitar
               com experiência interna registrada, mostre os dois lados e priorize a experiência interna
               na recomendação.
            8. Nunca invente casos, soluções, IDs, tecnologias, porcentagens, URLs ou trechos de página.
               Use apenas os números, scores e URLs retornados pelas ferramentas.
            9. Cite os casos pelo identificador CAS- seguido do número (ex.: CAS-182) e conhecimento
                pelo código retornado pela ferramenta (ex.: KB-004).
            10. Se não houver evidência suficiente após pesquisar, diga isso claramente antes de
               formular hipóteses.
            11. Embeddings/busca semântica não estão disponíveis neste fluxo — isso é esperado e não
                deve ser mencionado como erro ao usuário.
            12. Responda em português, de forma técnica, objetiva, e com "PRÓXIMOS PASSOS" ao final
                quando fizer sentido.
            """;
    }

    private async Task<(string Json, bool IsError)> ExecuteToolAsync(LlmToolCall call, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
            var args = doc.RootElement;

            switch (call.Name)
            {
                case "SearchCases":
                    return (await ExecuteSearchCasesAsync(args, ct), false);
                case "GetCaseDetails":
                    return await ExecuteGetCaseDetailsAsync(args, ct);
                case "SearchKnowledge":
                    return (await ExecuteSearchKnowledgeAsync(args, ct), false);
                case "GetKnowledgeDetails":
                    return await ExecuteGetKnowledgeDetailsAsync(args, ct);
                case "GetProductContext":
                    return await ExecuteGetProductContextAsync(args, ct);
                case "GetRelatedCases":
                    return await ExecuteGetRelatedCasesAsync(args, ct);
                case "SearchExternalSources":
                    return await ExecuteSearchExternalSourcesAsync(args, ct);
                default:
                    return (JsonSerializer.Serialize(new { error = $"Ferramenta '{call.Name}' não suportada neste fluxo." }), true);
            }
        }
        catch (Exception ex)
        {
            return (JsonSerializer.Serialize(new { error = ex.Message }), true);
        }
    }

    private async Task<string> ExecuteSearchCasesAsync(JsonElement args, CancellationToken ct)
    {
        string query = GetString(args, "query") ?? string.Empty;
        long? clientId = GetLong(args, "clientId");
        long? productId = GetLong(args, "productId");
        long? productVersionId = GetLong(args, "productVersionId");
        long? componentId = GetLong(args, "componentId");
        string? errorCode = GetString(args, "errorCode");
        bool resolvedOnly = args.TryGetProperty("resolvedOnly", out var ro) && ro.ValueKind == JsonValueKind.True;
        int pageSize = GetInt(args, "pageSize") ?? MaxSearchResultsPerCall;
        pageSize = Math.Min(pageSize, MaxSearchResultsPerCall);

        string? status = null;
        if (resolvedOnly) status = "Resolved";
        else if (args.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.Array)
        {
            status = st.EnumerateArray().Select(e => e.GetString()).FirstOrDefault();
        }

        var filters = new SearchFilterCriteriaDb(
            ProductId: productId,
            ProductVersionId: productVersionId,
            ClientId: clientId,
            ComponentId: componentId,
            ErrorCode: errorCode,
            Status: status);

        _strategies.Add(clientId.HasValue && productId.HasValue ? "StructuredSearch:ClientAndProduct"
            : productId.HasValue ? "StructuredSearch:Product"
            : !string.IsNullOrWhiteSpace(errorCode) ? "StructuredSearch:ErrorCode"
            : "TextSearch");

        var results = await _searchRepository.SearchCasesAsync(query, filters, pageSize, ct);

        var compact = results.Select(r => new
        {
            r.Id,
            CaseDisplay = $"CAS-{r.CaseNumber}",
            r.ClientName,
            r.ProductName,
            VersionLabel = r.VersionName,
            r.Status,
            r.Severity,
            r.ErrorCode,
            Summary = Truncate(r.NormalizedSummary ?? r.OriginalReport, 300)
        }).ToList();

        return JsonSerializer.Serialize(new { count = compact.Count, cases = compact });
    }

    private async Task<(string Json, bool IsError)> ExecuteGetCaseDetailsAsync(JsonElement args, CancellationToken ct)
    {
        long? caseId = GetLong(args, "caseId");
        if (!caseId.HasValue)
            return (JsonSerializer.Serialize(new { error = "caseId é obrigatório." }), true);

        if (_caseDetailCallsUsed >= MaxCaseDetailCalls)
            return (JsonSerializer.Serialize(new { error = $"Limite de {MaxCaseDetailCalls} casos detalhados por investigação atingido." }), true);

        var c = await _caseService.GetCaseByIdAsync(caseId.Value, ct);
        if (c == null)
            return (JsonSerializer.Serialize(new { error = $"Caso #{caseId} não encontrado." }), true);

        _caseDetailCallsUsed++;
        _strategies.Add("CaseDetail");

        var resolution = await _caseResolutionService.GetResolutionByCaseIdAsync(caseId.Value, ct);

        if (!_relatedCases.ContainsKey(c.Id))
        {
            _relatedCases[c.Id] = new RetrievedCaseDto(
                CaseId: c.Id,
                CaseDisplay: $"CAS-{c.CaseNumber}",
                ClientName: c.ClientName,
                ProductName: c.ProductName,
                VersionLabel: c.VersionLabel,
                Status: c.Status,
                Severity: c.Severity,
                Summary: c.NormalizedSummary ?? c.OriginalReport,
                ErrorCode: c.ErrorCode,
                ErrorMessage: c.ErrorMessage,
                Components: Array.Empty<string>(),
                ResolutionSummary: resolution?.ResolutionSummary,
                RootCauseSummary: resolution?.RootCauseName,
                MatchScore: null,
                MatchedFactors: Array.Empty<string>());
        }

        return (JsonSerializer.Serialize(new
        {
            CaseDisplay = $"CAS-{c.CaseNumber}",
            c.ClientName,
            c.ProductName,
            c.VersionLabel,
            c.Status,
            c.Severity,
            c.OriginalReport,
            c.NormalizedSummary,
            c.ErrorCode,
            c.ErrorMessage,
            Resolution = resolution == null ? null : new
            {
                resolution.ResolutionSummary,
                resolution.ValidationSummary,
                resolution.RootCauseName,
                resolution.RootCauseConfirmed
            },
            HasValidatedResolution = resolution != null
        }), false);
    }

    private async Task<string> ExecuteSearchKnowledgeAsync(JsonElement args, CancellationToken ct)
    {
        string? query = GetString(args, "query");
        long? productId = GetLong(args, "productId");

        _strategies.Add("KnowledgeSearch");

        // Camada 6 (§16): só conhecimento Published é apresentado como solução validada.
        var results = await _knowledgeService.SearchKnowledgeAsync(search: query, productId: productId, status: "Published", ct: ct);

        var compact = results.Take(MaxSearchResultsPerCall).Select(k => new
        {
            k.Id,
            k.Code,
            k.Title,
            Summary = Truncate(k.Summary, 300),
            k.Status
        }).ToList();

        return JsonSerializer.Serialize(new { count = compact.Count, knowledge = compact });
    }

    private async Task<(string Json, bool IsError)> ExecuteGetKnowledgeDetailsAsync(JsonElement args, CancellationToken ct)
    {
        long? knowledgeId = GetLong(args, "knowledgeId");
        if (!knowledgeId.HasValue)
            return (JsonSerializer.Serialize(new { error = "knowledgeId é obrigatório." }), true);

        var detail = await _knowledgeService.GetKnowledgeDetailAsync(knowledgeId.Value, ct);
        if (detail == null)
            return (JsonSerializer.Serialize(new { error = $"Conhecimento #{knowledgeId} não encontrado." }), true);

        _strategies.Add("KnowledgeSearch");

        bool isPublished = string.Equals(detail.LifecycleStatus, "Published", StringComparison.OrdinalIgnoreCase);
        if (isPublished && !_relatedKnowledge.ContainsKey(detail.Id))
        {
            _relatedKnowledge[detail.Id] = new RetrievedKnowledgeDto(detail.Id, detail.Code, detail.Title, Truncate(detail.ProblemDescription, 300), detail.LifecycleStatus);
        }

        return (JsonSerializer.Serialize(new
        {
            detail.Code,
            detail.Title,
            detail.ProblemDescription,
            detail.RootCauseSummary,
            Status = detail.LifecycleStatus,
            IsValidated = isPublished,
            Warning = isPublished ? null : "Este conteúdo NÃO é uma solução validada — status ainda é " + detail.LifecycleStatus + ". Não apresente como solução oficial."
        }), false);
    }

    private async Task<(string Json, bool IsError)> ExecuteGetProductContextAsync(JsonElement args, CancellationToken ct)
    {
        long? productId = GetLong(args, "productId");
        if (!productId.HasValue)
            return (JsonSerializer.Serialize(new { error = "productId é obrigatório." }), true);

        long? productVersionId = GetLong(args, "productVersionId");

        var context = await _technicalContextService.GetInvestigationContextAsync(productId.Value, productVersionId, ct);
        if (context == null)
            return (JsonSerializer.Serialize(new { error = $"Produto #{productId} não encontrado." }), true);

        _usedTechnicalContext = true;
        _strategies.Add("TechnicalContext");

        return (JsonSerializer.Serialize(new
        {
            context.ProductName,
            context.ProductDescription,
            BusinessPurpose = context.TechnicalProfile?.BusinessPurpose,
            ArchitectureSummary = context.TechnicalProfile?.ArchitectureSummary,
            context.Technologies,
            Components = context.Components.Select(c => c.Name),
            Dependencies = context.Dependencies.Select(d => $"{d.SourceComponentName} -> {d.TargetComponentName} ({d.DependencyType})"),
            Integrations = context.Integrations.Select(i => i.Name),
            HasTechnicalProfile = context.TechnicalProfile != null,
            Note = context.TechnicalProfile == null
                ? "Nenhum contexto técnico cadastrado para este produto — não invente arquitetura."
                : null
        }), false);
    }

    private async Task<(string Json, bool IsError)> ExecuteGetRelatedCasesAsync(JsonElement args, CancellationToken ct)
    {
        long? caseId = GetLong(args, "caseId");
        if (!caseId.HasValue)
            return (JsonSerializer.Serialize(new { error = "caseId é obrigatório." }), true);

        _strategies.Add("CaseSimilarity");

        var related = await _caseRelationService.ComputeSimilarCasesAsync(caseId.Value, ct);

        foreach (var r in related)
        {
            if (!_relatedCases.ContainsKey(r.TargetCaseId))
            {
                _relatedCases[r.TargetCaseId] = new RetrievedCaseDto(
                    CaseId: r.TargetCaseId,
                    CaseDisplay: $"CAS-{r.TargetCaseNumber}",
                    ClientName: null,
                    ProductName: r.TargetProductName,
                    VersionLabel: null,
                    Status: r.TargetStatus,
                    Severity: r.TargetSeverity,
                    Summary: r.TargetTitle,
                    ErrorCode: null,
                    ErrorMessage: null,
                    Components: r.TargetComponentName != null ? new[] { r.TargetComponentName } : Array.Empty<string>(),
                    ResolutionSummary: null,
                    RootCauseSummary: null,
                    MatchScore: r.SimilarityScore,
                    MatchedFactors: r.MatchedFactors);
            }
        }

        var compact = related.Select(r => new
        {
            CaseDisplay = $"CAS-{r.TargetCaseNumber}",
            r.TargetStatus,
            r.SimilarityScore,
            r.MatchedFactors
        });

        return (JsonSerializer.Serialize(new { count = related.Count, relatedCases = compact }), false);
    }

    private async Task<(string Json, bool IsError)> ExecuteSearchExternalSourcesAsync(JsonElement args, CancellationToken ct)
    {
        long? productId = GetLong(args, "productId");
        string? query = GetString(args, "query");

        if (!productId.HasValue || string.IsNullOrWhiteSpace(query))
            return (JsonSerializer.Serialize(new { error = "productId e query são obrigatórios." }), true);

        // §71/§72: pesquisa externa entra no mesmo limite total de 8 tool calls, com
        // um sub-limite de 3 pesquisas externas — nunca um limite separado que permita
        // ultrapassar o total.
        if (_externalSearchCallsUsed >= MaxExternalSearchCalls)
        {
            return (JsonSerializer.Serialize(new { error = $"Limite de {MaxExternalSearchCalls} pesquisas externas por investigação atingido." }), true);
        }

        int maxResults = GetInt(args, "maxResults") ?? 5;

        _externalSearchCallsUsed++;

        // O LLM nunca decide política/domínios permitidos (§19/§20) — só passa
        // productId/query; IExternalResearchService carrega e aplica a política real.
        var outcome = await _externalResearchService.SearchAsync(productId.Value, query, maxResults, userId: null, ct);

        if (outcome.Rejected)
        {
            // §6/§62: policy Disabled ou provider ausente não é erro fatal — o modelo
            // recebe a razão e segue a investigação só com o que já tem.
            return (JsonSerializer.Serialize(new { rejected = true, reason = outcome.RejectionReason }), false);
        }

        _strategies.Add("ExternalResearch");

        foreach (var r in outcome.Results)
        {
            _externalSourcesUsed.TryAdd(r.Url, new RetrievedExternalSourceDto(r.Title, r.Url, r.Domain, r.TrustLevel));
        }

        var compact = outcome.Results.Select(r => new
        {
            r.Title,
            r.Url,
            r.Domain,
            r.TrustLevel,
            Snippet = Truncate(r.Snippet, 300)
        });

        return (JsonSerializer.Serialize(new
        {
            count = outcome.Results.Count,
            policy = outcome.PolicyApplied,
            results = compact,
            Note = "Rotule esta fonte como DOCUMENTAÇÃO EXTERNA na resposta, nunca como fato interno da empresa."
        }), false);
    }

    private static string? GetString(JsonElement args, string name) =>
        args.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static long? GetLong(JsonElement args, string name) =>
        args.TryGetProperty(name, out var v) && v.TryGetInt64(out var l) ? l : null;

    private static int? GetInt(JsonElement args, string name) =>
        args.TryGetProperty(name, out var v) && v.TryGetInt32(out var i) ? i : null;

    private static string Truncate(string? text, int max)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= max ? text : text[..max] + "...";
    }
}
