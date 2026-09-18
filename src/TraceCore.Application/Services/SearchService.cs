using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class SearchService : ISearchService
{
    private readonly ISearchRepository _searchRepository;
    private readonly ICaseRepository _caseRepository;

    public SearchService(
        ISearchRepository searchRepository,
        ICaseRepository caseRepository)
    {
        _searchRepository = searchRepository;
        _caseRepository = caseRepository;
    }

    public async Task<SearchResultsResponseDto> SearchAsync(ExecuteSearchCommand command, long? currentUserId = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var queryText = command.QueryText?.Trim() ?? string.Empty;
        var rawFilters = command.Filters ?? new SearchFilterCriteria();

        // 1. Resolução de Contexto (ContextCaseId)
        long? contextProductId = null;
        long? contextComponentId = null;
        long? contextProductVersionId = null;
        string? contextErrorCode = null;

        var activeChips = new List<SearchFilterChipDto>();

        if (command.ContextCaseId.HasValue)
        {
            var ctxCase = await _caseRepository.GetByIdAsync(command.ContextCaseId.Value, ct);
            if (ctxCase != null)
            {
                contextProductId = ctxCase.ProductId;
                contextComponentId = ctxCase.AffectedComponents.FirstOrDefault()?.ComponentId;
                contextProductVersionId = ctxCase.ProductVersionId;
                contextErrorCode = ctxCase.ErrorCode;

                activeChips.Add(new SearchFilterChipDto(
                    "contextCaseId",
                    $"Caso Contexto #{ctxCase.Id}",
                    ctxCase.Id.ToString()
                ));
            }
        }

        // Adiciona chips de filtros manuais ativos (BR-063)
        if (rawFilters.ProductId.HasValue)
            activeChips.Add(new SearchFilterChipDto("productId", $"Produto: {rawFilters.ProductName ?? rawFilters.ProductId.ToString()}", rawFilters.ProductId.Value.ToString()));
        if (rawFilters.ComponentId.HasValue)
            activeChips.Add(new SearchFilterChipDto("componentId", $"Componente: {rawFilters.ComponentName ?? rawFilters.ComponentId.ToString()}", rawFilters.ComponentId.Value.ToString()));
        if (rawFilters.ProductVersionId.HasValue)
            activeChips.Add(new SearchFilterChipDto("productVersionId", $"Versão: {rawFilters.VersionName ?? rawFilters.ProductVersionId.ToString()}", rawFilters.ProductVersionId.Value.ToString()));
        if (rawFilters.ClientId.HasValue)
            activeChips.Add(new SearchFilterChipDto("clientId", $"Cliente: {rawFilters.ClientName ?? rawFilters.ClientId.ToString()}", rawFilters.ClientId.Value.ToString()));
        if (rawFilters.ClientUnitId.HasValue)
            activeChips.Add(new SearchFilterChipDto("clientUnitId", $"Unidade: {rawFilters.ClientUnitName ?? rawFilters.ClientUnitId.ToString()}", rawFilters.ClientUnitId.Value.ToString()));
        if (rawFilters.TechnologyId.HasValue)
            activeChips.Add(new SearchFilterChipDto("technologyId", $"Tecnologia: {rawFilters.TechnologyName ?? rawFilters.TechnologyId.ToString()}", rawFilters.TechnologyId.Value.ToString()));
        if (!string.IsNullOrWhiteSpace(rawFilters.ErrorCode))
            activeChips.Add(new SearchFilterChipDto("errorCode", $"Erro: {rawFilters.ErrorCode}", rawFilters.ErrorCode));
        if (rawFilters.RootCauseId.HasValue)
            activeChips.Add(new SearchFilterChipDto("rootCauseId", $"Causa: {rawFilters.RootCauseName ?? rawFilters.RootCauseId.ToString()}", rawFilters.RootCauseId.Value.ToString()));
        if (!string.IsNullOrWhiteSpace(rawFilters.Status))
            activeChips.Add(new SearchFilterChipDto("status", $"Status: {rawFilters.Status}", rawFilters.Status));
        if (!string.IsNullOrWhiteSpace(rawFilters.Environment))
            activeChips.Add(new SearchFilterChipDto("environment", $"Ambiente: {rawFilters.Environment}", rawFilters.Environment));

        // 2. Prepara filtros para Repositório
        var dbFilters = new SearchFilterCriteriaDb(
            ProductId: rawFilters.ProductId,
            ProductVersionId: rawFilters.ProductVersionId,
            ClientId: rawFilters.ClientId,
            ClientUnitId: rawFilters.ClientUnitId,
            DepartmentId: rawFilters.DepartmentId,
            TechnologyId: rawFilters.TechnologyId,
            ComponentId: rawFilters.ComponentId,
            StartDate: rawFilters.StartDate,
            EndDate: rawFilters.EndDate,
            ErrorCode: rawFilters.ErrorCode,
            RootCauseId: rawFilters.RootCauseId,
            Status: rawFilters.Status,
            Environment: rawFilters.Environment
        );

        // 3. Execução Federada de Sub-Consultas
        var caseTask = _searchRepository.SearchCasesAsync(queryText, dbFilters, 50, ct);
        var solutionTask = _searchRepository.SearchSolutionsAsync(queryText, dbFilters, allowDrafts: true, 50, ct);
        var productTask = _searchRepository.SearchProductsAsync(queryText, 20, ct);
        var componentTask = _searchRepository.SearchComponentsAsync(queryText, rawFilters.ProductId, 20, ct);
        var rootCauseTask = _searchRepository.SearchRootCausesAsync(queryText, 20, ct);

        await Task.WhenAll(caseTask, solutionTask, productTask, componentTask, rootCauseTask);

        var rawCases = await caseTask;
        var rawSolutions = await solutionTask;
        var rawProducts = await productTask;
        var rawComponents = await componentTask;
        var rawRootCauses = await rootCauseTask;

        var allItems = new List<SearchResultItemDto>();

        // 4. Transformação e Ranking de CASOS
        foreach (var c in rawCases)
        {
            var factors = new List<string>();
            double score = c.TextScore;

            if (!string.IsNullOrWhiteSpace(queryText))
            {
                if (c.CaseNumber.Contains(queryText, StringComparison.OrdinalIgnoreCase))
                {
                    score += 40;
                    factors.Add("Código exato do caso");
                }
                else if (c.NormalizedSummary?.Contains(queryText, StringComparison.OrdinalIgnoreCase) == true)
                {
                    factors.Add("Termo no resumo");
                }
            }

            // Boosts Contextuais
            if (contextProductId.HasValue && c.ProductId == contextProductId.Value)
            {
                score += 30;
                factors.Add("Mesmo sistema");
            }
            if (contextComponentId.HasValue && c.ComponentId == contextComponentId.Value)
            {
                score += 25;
                factors.Add("Mesmo componente");
            }
            if (contextProductVersionId.HasValue && c.ProductVersionId == contextProductVersionId.Value)
            {
                score += 20;
                factors.Add("Mesma versão");
            }
            if (!string.IsNullOrWhiteSpace(contextErrorCode) && !string.IsNullOrWhiteSpace(c.ErrorCode) &&
                string.Equals(c.ErrorCode, contextErrorCode, StringComparison.OrdinalIgnoreCase))
            {
                score += 35;
                factors.Add($"Mesmo erro ({c.ErrorCode})");
            }

            if (factors.Count == 0) factors.Add("Relevância textual");

            var snippet = !string.IsNullOrWhiteSpace(c.NormalizedSummary) ? c.NormalizedSummary : c.OriginalReport;
            if (snippet.Length > 180) snippet = snippet[..180] + "...";

            allItems.Add(new SearchResultItemDto(
                Type: "Case",
                Id: c.Id,
                Code: c.CaseNumber,
                Title: c.NormalizedSummary ?? c.CaseNumber,
                Subtitle: $"{c.ProductName ?? "Sistema N/A"} • Versão {c.VersionName ?? "N/A"} • {c.ComponentName ?? "Componente N/A"}",
                Snippet: snippet,
                Status: c.Status,
                Score: Math.Round(score, 1),
                MatchedFactors: factors,
                SuccessRate: null,
                SuccessSample: null,
                Url: $"/Cases/Details/{c.Id}",
                Position: 0
            ));
        }

        // 5. Transformação e Ranking de SOLUÇÕES
        foreach (var s in rawSolutions)
        {
            var factors = new List<string>();
            double score = s.TextScore;

            // BR-062: Penalidade/Exclusão se explicitamente incompatível
            if (rawFilters.ProductVersionId.HasValue && s.NegativeProductVersionIds.Contains(rawFilters.ProductVersionId.Value))
            {
                score -= 100; // Penalidade forte
                factors.Add("Incompatibilidade declarada de versão");
            }

            if (!string.IsNullOrWhiteSpace(queryText))
            {
                if (s.KnowledgeCode.Contains(queryText, StringComparison.OrdinalIgnoreCase))
                {
                    score += 40;
                    factors.Add("Código da solução");
                }
                else if (s.Title.Contains(queryText, StringComparison.OrdinalIgnoreCase))
                {
                    factors.Add("Termo no título");
                }
            }

            // Boosts Contextuais
            if (contextProductId.HasValue && s.ApplicableProductIds.Contains(contextProductId.Value))
            {
                score += 30;
                factors.Add("Mesmo sistema");
            }
            if (contextComponentId.HasValue && s.ApplicableComponentIds.Contains(contextComponentId.Value))
            {
                score += 25;
                factors.Add("Mesmo componente");
            }

            // Boost de Eficácia Histórica (BR-047 / BR-048)
            string? rateStr = null;
            string? sampleStr = null;
            if (s.TotalUsages > 0)
            {
                double eff = (double)s.SuccessfulUsages / s.TotalUsages;
                score += eff * 25.0; // Até +25 pontos
                score += Math.Min(s.TotalUsages * 2.0, 20.0); // Frequência até +20 pontos

                rateStr = $"{Math.Round(eff * 100.0, 1):F1}%";
                sampleStr = $"({s.TotalUsages} {(s.TotalUsages == 1 ? "caso" : "casos")})";
                factors.Add($"Taxa de eficácia: {rateStr} {sampleStr}");
            }

            if (factors.Count == 0) factors.Add("Relevância técnica");

            var snippet = !string.IsNullOrWhiteSpace(s.ProblemDescription) ? s.ProblemDescription : s.Summary;
            if (snippet.Length > 180) snippet = snippet[..180] + "...";

            allItems.Add(new SearchResultItemDto(
                Type: "Solution",
                Id: s.Id,
                Code: s.KnowledgeCode,
                Title: s.Title,
                Subtitle: $"Procedimento Oficial • Versão {s.Version} • Status: {s.Status}",
                Snippet: snippet,
                Status: s.Status,
                Score: Math.Round(score, 1),
                MatchedFactors: factors,
                SuccessRate: rateStr,
                SuccessSample: sampleStr,
                Url: $"/Knowledge/Details/{s.Id}",
                Position: 0
            ));
        }

        // 6. Transformação de SISTEMAS (Products)
        foreach (var p in rawProducts)
        {
            var factors = new List<string>();
            double score = p.TextScore;
            if (!string.IsNullOrWhiteSpace(queryText))
            {
                if (string.Equals(p.Code, queryText, StringComparison.OrdinalIgnoreCase)) { factors.Add("Código exato do sistema"); score += 50; }
                else if (p.Code.Contains(queryText, StringComparison.OrdinalIgnoreCase)) { factors.Add("Código do sistema"); score += 30; }
                if (string.Equals(p.Name, queryText, StringComparison.OrdinalIgnoreCase)) { factors.Add("Nome exato do sistema"); score += 45; }
                else if (p.Name.Contains(queryText, StringComparison.OrdinalIgnoreCase)) { factors.Add("Nome do sistema"); score += 25; }
                if (p.Description?.Contains(queryText, StringComparison.OrdinalIgnoreCase) == true) { factors.Add("Descrição do sistema"); score += 15; }
            }
            if (factors.Count == 0) factors.Add("Catálogo de Sistemas");

            allItems.Add(new SearchResultItemDto(
                Type: "System",
                Id: p.Id,
                Code: p.Code,
                Title: p.Name,
                Subtitle: "Sistema Corporativo do Catálogo",
                Snippet: p.Description ?? "Sistema catalogado na base técnica",
                Status: "Ativo",
                Score: Math.Round(score, 1),
                MatchedFactors: factors,
                SuccessRate: null,
                SuccessSample: null,
                Url: $"/Search?productId={p.Id}&productName={Uri.EscapeDataString(p.Name)}",
                Position: 0
            ));
        }

        // 7. Transformação de COMPONENTES
        foreach (var comp in rawComponents)
        {
            var factors = new List<string>();
            double score = comp.TextScore;
            if (!string.IsNullOrWhiteSpace(queryText))
            {
                if (string.Equals(comp.Name, queryText, StringComparison.OrdinalIgnoreCase)) { factors.Add("Nome exato do componente"); score += 50; }
                else if (comp.Name.Contains(queryText, StringComparison.OrdinalIgnoreCase)) { factors.Add("Nome do componente"); score += 30; }
                if (comp.Description?.Contains(queryText, StringComparison.OrdinalIgnoreCase) == true) { factors.Add("Descrição do componente"); score += 15; }
                if (comp.Technology?.Contains(queryText, StringComparison.OrdinalIgnoreCase) == true) { factors.Add($"Tecnologia ({comp.Technology})"); score += 20; }
            }
            if (factors.Count == 0) factors.Add("Módulo / Componente");

            allItems.Add(new SearchResultItemDto(
                Type: "Component",
                Id: comp.Id,
                Code: $"CMP-{comp.Id}",
                Title: comp.Name,
                Subtitle: $"{comp.ProductName} • Tecnologia: {comp.Technology ?? "Geral"}",
                Snippet: comp.Description ?? $"Módulo técnico do sistema {comp.ProductName}",
                Status: "Ativo",
                Score: Math.Round(score, 1),
                MatchedFactors: factors,
                SuccessRate: null,
                SuccessSample: null,
                Url: $"/Search?componentId={comp.Id}&componentName={Uri.EscapeDataString(comp.Name)}",
                Position: 0
            ));
        }

        // 8. Transformação de CAUSAS RAIZ
        foreach (var rc in rawRootCauses)
        {
            var factors = new List<string>();
            double score = rc.TextScore;
            if (!string.IsNullOrWhiteSpace(queryText))
            {
                if (string.Equals(rc.Name, queryText, StringComparison.OrdinalIgnoreCase)) { factors.Add("Nome exato da causa raiz"); score += 50; }
                else if (rc.Name.Contains(queryText, StringComparison.OrdinalIgnoreCase)) { factors.Add("Nome da causa raiz"); score += 30; }
                if (rc.Category.Contains(queryText, StringComparison.OrdinalIgnoreCase)) { factors.Add($"Categoria ({rc.Category})"); score += 20; }
                if (rc.Description?.Contains(queryText, StringComparison.OrdinalIgnoreCase) == true) { factors.Add("Descrição da causa raiz"); score += 15; }
            }
            if (factors.Count == 0) factors.Add("Taxonomia de Causa Raiz");

            allItems.Add(new SearchResultItemDto(
                Type: "RootCause",
                Id: rc.Id,
                Code: $"RC-{rc.Id}",
                Title: rc.Name,
                Subtitle: $"Causa Raiz Corporativa • Categoria: {rc.Category}",
                Snippet: rc.Description ?? "Padrão de causa raiz estruturado",
                Status: rc.Category,
                Score: Math.Round(rc.TextScore, 1),
                MatchedFactors: factors,
                SuccessRate: null,
                SuccessSample: null,
                Url: $"/Search?rootCauseId={rc.Id}&rootCauseName={Uri.EscapeDataString(rc.Name)}",
                Position: 0
            ));
        }

        // 8.1. Reconhecimento de Identificador Técnico e Correspondência Exata (Bloco 7.A.6)
        var exactMatches = new List<SearchResultItemDto>();
        if (!string.IsNullOrWhiteSpace(queryText))
        {
            foreach (var item in allItems)
            {
                bool isExact = false;
                var exactFactors = new List<string>();

                if (item.Type == "Case")
                {
                    var rawCase = rawCases.FirstOrDefault(c => c.Id == item.Id);
                    if (rawCase != null)
                    {
                        if (string.Equals(rawCase.CaseNumber, queryText, StringComparison.OrdinalIgnoreCase))
                        {
                            isExact = true;
                            exactFactors.Add("Número exato do caso");
                        }
                        else if (!string.IsNullOrWhiteSpace(rawCase.ErrorCode) && string.Equals(rawCase.ErrorCode, queryText, StringComparison.OrdinalIgnoreCase))
                        {
                            isExact = true;
                            exactFactors.Add($"Código de erro exato ({rawCase.ErrorCode})");
                        }
                    }
                }
                else if (item.Type == "Solution")
                {
                    var rawSol = rawSolutions.FirstOrDefault(s => s.Id == item.Id);
                    if (rawSol != null && string.Equals(rawSol.KnowledgeCode, queryText, StringComparison.OrdinalIgnoreCase))
                    {
                        isExact = true;
                        exactFactors.Add("Código exato da solução");
                    }
                }
                else if (item.Type == "System")
                {
                    var rawProd = rawProducts.FirstOrDefault(p => p.Id == item.Id);
                    if (rawProd != null && (string.Equals(rawProd.Code, queryText, StringComparison.OrdinalIgnoreCase) || string.Equals(rawProd.Name, queryText, StringComparison.OrdinalIgnoreCase)))
                    {
                        isExact = true;
                        exactFactors.Add(string.Equals(rawProd.Code, queryText, StringComparison.OrdinalIgnoreCase) ? "Código exato do sistema" : "Nome exato do sistema");
                    }
                }
                else if (item.Type == "Component")
                {
                    var rawComp = rawComponents.FirstOrDefault(c => c.Id == item.Id);
                    if (rawComp != null && (string.Equals(rawComp.Name, queryText, StringComparison.OrdinalIgnoreCase) || string.Equals($"CMP-{rawComp.Id}", queryText, StringComparison.OrdinalIgnoreCase)))
                    {
                        isExact = true;
                        exactFactors.Add("Identificador exato do componente");
                    }
                }
                else if (item.Type == "RootCause")
                {
                    var rawRc = rawRootCauses.FirstOrDefault(r => r.Id == item.Id);
                    if (rawRc != null && (string.Equals(rawRc.Name, queryText, StringComparison.OrdinalIgnoreCase) || string.Equals($"RC-{rawRc.Id}", queryText, StringComparison.OrdinalIgnoreCase)))
                    {
                        isExact = true;
                        exactFactors.Add("Nome exato da causa raiz");
                    }
                }

                if (isExact)
                {
                    exactMatches.Add(item with
                    {
                        Score = 100.0,
                        MatchedFactors = exactFactors.Concat(item.MatchedFactors.Where(f => !exactFactors.Contains(f))).ToList()
                    });
                }
            }
        }

        // 9. Contadores por Tipo para as Tabs
        var countByType = new Dictionary<string, int>
        {
            ["All"] = allItems.Count,
            ["Solution"] = allItems.Count(i => i.Type == "Solution"),
            ["Case"] = allItems.Count(i => i.Type == "Case"),
            ["System"] = allItems.Count(i => i.Type == "System"),
            ["Component"] = allItems.Count(i => i.Type == "Component"),
            ["RootCause"] = allItems.Count(i => i.Type == "RootCause")
        };

        // 10. Filtragem por Tipo Selecionado
        var selectedType = command.SelectedType ?? "All";
        var filteredItems = selectedType switch
        {
            "Solution" => allItems.Where(i => i.Type == "Solution").ToList(),
            "Case" => allItems.Where(i => i.Type == "Case").ToList(),
            "System" => allItems.Where(i => i.Type == "System").ToList(),
            "Component" => allItems.Where(i => i.Type == "Component").ToList(),
            "RootCause" => allItems.Where(i => i.Type == "RootCause").ToList(),
            _ => allItems
        };

        // 11. Ordenação Decrescente por Score
        var rankedItems = filteredItems
            .OrderByDescending(i => i.Score)
            .ToList();

        // Atribui posições (1-indexed)
        for (int idx = 0; idx < rankedItems.Count; idx++)
        {
            rankedItems[idx] = rankedItems[idx] with { Position = idx + 1 };
        }

        sw.Stop();
        var durationMs = (int)sw.ElapsedMilliseconds;

        // 12. Persistência de Auditoria / Métricas (BR-064 e BR-065)
        long queryId = 0;
        try
        {
            var session = new SearchSession(currentUserId, command.ContextCaseId.HasValue ? JsonSerializer.Serialize(new { command.ContextCaseId }) : null);
            var sessionId = await _searchRepository.CreateSessionAsync(session, ct);

            var queryRecord = new SearchQueryRecord(
                searchSessionId: sessionId,
                queryText: queryText,
                filtersJson: JsonSerializer.Serialize(dbFilters),
                resultCount: allItems.Count, // Persiste mesmo quando 0 (BR-065)
                durationMs: durationMs
            );
            queryId = await _searchRepository.RecordQueryAsync(queryRecord, ct);
        }
        catch
        {
            // Falhas de telemetria não devem abortar a exibição de resultados
        }

        return new SearchResultsResponseDto(
            QueryId: queryId,
            QueryText: queryText,
            DurationMs: durationMs,
            TotalCount: allItems.Count,
            Items: rankedItems,
            CountByType: countByType,
            ActiveFilterChips: activeChips,
            ExactMatches: exactMatches
        );
    }

    public async Task<long> RecordInteractionAsync(RecordResultInteractionCommand command, CancellationToken ct = default)
    {
        var interaction = new SearchResultInteraction(
            searchQueryId: command.QueryId,
            resultType: command.ResultType,
            resultId: command.ResultId,
            position: command.Position
        );
        interaction.MarkOpened();

        return await _searchRepository.RecordInteractionAsync(interaction, ct);
    }

    public async Task RecordFeedbackAsync(RecordFeedbackCommand command, CancellationToken ct = default)
    {
        await _searchRepository.RecordFeedbackAsync(command.InteractionId, command.Useful, ct);
    }
}
