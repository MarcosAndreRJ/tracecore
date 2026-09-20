using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class CaseRelationService : ICaseRelationService
{
    private readonly ICaseRelationRepository _caseRelationRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IAuditEventRepository _auditEventRepository;
    private readonly IUserRepository _userRepository;

    public CaseRelationService(
        ICaseRelationRepository caseRelationRepository,
        ICaseRepository caseRepository,
        ICatalogRepository catalogRepository,
        IAuditEventRepository auditEventRepository,
        IUserRepository userRepository)
    {
        _caseRelationRepository = caseRelationRepository;
        _caseRepository = caseRepository;
        _catalogRepository = catalogRepository;
        _auditEventRepository = auditEventRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<CaseRelationDto>> ComputeSimilarCasesAsync(long caseId, CancellationToken ct = default)
    {
        var sourceCase = await _caseRepository.GetByIdAsync(caseId, ct);
        if (sourceCase == null) return new List<CaseRelationDto>();

        var candidates = await _caseRelationRepository.GetPotentialSimilarCandidatesAsync(
            excludeCaseId: caseId,
            clientId: sourceCase.ClientId,
            productId: sourceCase.ProductId,
            errorCode: sourceCase.ErrorCode,
            limit: 50,
            ct: ct
        );

        var scoredRelations = new List<CaseRelation>();
        var sourceCompIds = sourceCase.AffectedComponents.Select(c => c.ComponentId).ToHashSet();
        var sourceWords = ExtractSignificantWords(sourceCase.NormalizedSummary ?? sourceCase.OriginalReport);

        foreach (var candidate in candidates)
        {
            double score = 0;
            var factors = new List<string>();

            // 0. Mesmo cliente (+15) — Camada 1 (Prompt 3): reforça candidatos do
            // mesmo cliente quando combinados com outros fatores técnicos.
            if (sourceCase.ClientId.HasValue && candidate.ClientId.HasValue && sourceCase.ClientId.Value == candidate.ClientId.Value)
            {
                score += 15;
                factors.Add("Mesmo cliente");
            }

            // 1. Mesmo produto (+30)
            if (sourceCase.ProductId.HasValue && candidate.ProductId.HasValue && sourceCase.ProductId.Value == candidate.ProductId.Value)
            {
                score += 30;
                factors.Add("Mesmo produto");
            }

            // 2. Mesmo componente (+25)
            var candidateCompIds = candidate.AffectedComponents.Select(c => c.ComponentId).ToHashSet();
            if (sourceCompIds.Count > 0 && sourceCompIds.Overlaps(candidateCompIds))
            {
                score += 25;
                factors.Add("Mesmo componente");
            }

            // 3. Mesma versão (+20)
            if (sourceCase.ProductVersionId.HasValue && candidate.ProductVersionId.HasValue && sourceCase.ProductVersionId.Value == candidate.ProductVersionId.Value)
            {
                score += 20;
                factors.Add("Mesma versão");
            }

            // 4. Mesmo código de erro (+35)
            if (!string.IsNullOrWhiteSpace(sourceCase.ErrorCode) && !string.IsNullOrWhiteSpace(candidate.ErrorCode) &&
                string.Equals(sourceCase.ErrorCode, candidate.ErrorCode, StringComparison.OrdinalIgnoreCase))
            {
                score += 35;
                factors.Add($"Mesmo erro ({candidate.ErrorCode})");
            }

            // 5. Correspondência textual de palavras-chave no resumo / relato (+10 a +20)
            var candidateWords = ExtractSignificantWords(candidate.NormalizedSummary ?? candidate.OriginalReport);
            int overlapWords = sourceWords.Intersect(candidateWords, StringComparer.OrdinalIgnoreCase).Count();
            if (overlapWords > 0)
            {
                score += Math.Min(overlapWords * 5.0, 20.0);
                factors.Add("Termos semelhantes no relato");
            }

            if (score <= 0) continue;

            // Princípio P-006: Similaridade é score determinístico, não probabilidade estatística.
            double normalizedScore = Math.Min(100.0, Math.Round(score, 1));

            var relation = new CaseRelation(
                sourceCaseId: sourceCase.Id,
                targetCaseId: candidate.Id,
                relationType: CaseRelationType.Similar,
                similarityScore: normalizedScore,
                matchedFactors: factors
            );

            scoredRelations.Add(relation);
        }

        // Seleciona top 5 similares
        var topSimilar = scoredRelations
            .OrderByDescending(r => r.SimilarityScore)
            .Take(5)
            .ToList();

        // Persistência idempotente
        await _caseRelationRepository.SaveSimilarRelationsAsync(sourceCase.Id, topSimilar, ct);

        // Mapeia para DTOs
        var dtos = new List<CaseRelationDto>();
        foreach (var rel in topSimilar)
        {
            var target = candidates.FirstOrDefault(c => c.Id == rel.TargetCaseId);
            if (target != null)
            {
                dtos.Add(new CaseRelationDto(
                    Id: rel.Id,
                    SourceCaseId: rel.SourceCaseId,
                    TargetCaseId: rel.TargetCaseId,
                    TargetCaseNumber: target.CaseNumber,
                    TargetTitle: target.NormalizedSummary ?? target.OriginalReport,
                    TargetStatus: target.Status,
                    TargetSeverity: target.Severity,
                    TargetProductName: null,
                    TargetComponentName: null,
                    RelationType: rel.RelationType,
                    SimilarityScore: rel.SimilarityScore,
                    MatchedFactors: rel.MatchedFactors,
                    CreatedBy: rel.CreatedBy,
                    CreatedByName: "Sistema (Automático)",
                    CreatedAt: rel.CreatedAt
                ));
            }
        }

        return dtos;
    }

    public async Task<CaseRelationsOverviewDto> GetCaseRelationsOverviewAsync(long caseId, CancellationToken ct = default)
    {
        var sourceCase = await _caseRepository.GetByIdAsync(caseId, ct);
        if (sourceCase == null)
        {
            return new CaseRelationsOverviewDto(caseId, new List<CaseRelationDto>(), new List<CaseRelationDto>(), null);
        }

        // Obtém relações existentes
        var allRelations = await _caseRelationRepository.GetRelationsByCaseIdAsync(caseId, ct);

        var similarEntities = allRelations.Where(r => r.RelationType == CaseRelationType.Similar.ToString()).ToList();

        // Se o caso estiver em investigação (Open / Reopened / Investigating), revalida/computa similares
        if (sourceCase.Status == "Open" || sourceCase.Status == "Reopened" || sourceCase.Status == "Investigating")
        {
            await ComputeSimilarCasesAsync(caseId, ct);
            allRelations = await _caseRelationRepository.GetRelationsByCaseIdAsync(caseId, ct);
            similarEntities = allRelations.Where(r => r.RelationType == CaseRelationType.Similar.ToString()).ToList();
        }

        var manualEntities = allRelations.Where(r => r.RelationType != CaseRelationType.Similar.ToString()).ToList();

        // Coleta dados dos casos alvos
        var allTargetIds = allRelations
            .Select(r => r.SourceCaseId == caseId ? r.TargetCaseId : r.SourceCaseId)
            .Distinct()
            .ToList();

        var targetCases = new Dictionary<long, Case>();
        foreach (var tid in allTargetIds)
        {
            var tc = await _caseRepository.GetByIdAsync(tid, ct);
            if (tc != null) targetCases[tid] = tc;
        }

        // Busca produtos para exibir nomes
        var products = await _catalogRepository.GetAllProductsAsync(ct);
        var prodLookup = products.ToDictionary(p => p.Id, p => p.Name);

        var components = await _catalogRepository.GetAllComponentsAsync(ct: ct);
        var compLookup = components.ToDictionary(c => c.Id, c => c.Name);

        var users = await _userRepository.GetAllAsync(ct);
        var userLookup = users.ToDictionary(u => u.Id, u => u.Name);

        var similarDtos = new List<CaseRelationDto>();
        foreach (var rel in similarEntities)
        {
            long targetId = rel.SourceCaseId == caseId ? rel.TargetCaseId : rel.SourceCaseId;
            if (targetCases.TryGetValue(targetId, out var target))
            {
                string? prodName = target.ProductId.HasValue && prodLookup.TryGetValue(target.ProductId.Value, out var pn) ? pn : null;
                var compId = target.AffectedComponents.FirstOrDefault()?.ComponentId;
                string? compName = compId.HasValue && compLookup.TryGetValue(compId.Value, out var cn) ? cn : null;

                similarDtos.Add(new CaseRelationDto(
                    Id: rel.Id,
                    SourceCaseId: rel.SourceCaseId,
                    TargetCaseId: target.Id,
                    TargetCaseNumber: target.CaseNumber,
                    TargetTitle: target.NormalizedSummary ?? (target.OriginalReport.Length > 100 ? target.OriginalReport[..100] + "..." : target.OriginalReport),
                    TargetStatus: target.Status,
                    TargetSeverity: target.Severity,
                    TargetProductName: prodName,
                    TargetComponentName: compName,
                    RelationType: rel.RelationType,
                    SimilarityScore: rel.SimilarityScore,
                    MatchedFactors: rel.MatchedFactors,
                    CreatedBy: rel.CreatedBy,
                    CreatedByName: "Sistema (Automático)",
                    CreatedAt: rel.CreatedAt
                ));
            }
        }

        var manualDtos = new List<CaseRelationDto>();
        foreach (var rel in manualEntities)
        {
            long targetId = rel.SourceCaseId == caseId ? rel.TargetCaseId : rel.SourceCaseId;
            if (targetCases.TryGetValue(targetId, out var target))
            {
                string? prodName = target.ProductId.HasValue && prodLookup.TryGetValue(target.ProductId.Value, out var pn) ? pn : null;
                var compId = target.AffectedComponents.FirstOrDefault()?.ComponentId;
                string? compName = compId.HasValue && compLookup.TryGetValue(compId.Value, out var cn) ? cn : null;

                string createdByName = rel.CreatedBy.HasValue && userLookup.TryGetValue(rel.CreatedBy.Value, out var un)
                    ? un
                    : "Usuário";

                manualDtos.Add(new CaseRelationDto(
                    Id: rel.Id,
                    SourceCaseId: rel.SourceCaseId,
                    TargetCaseId: target.Id,
                    TargetCaseNumber: target.CaseNumber,
                    TargetTitle: target.NormalizedSummary ?? (target.OriginalReport.Length > 100 ? target.OriginalReport[..100] + "..." : target.OriginalReport),
                    TargetStatus: target.Status,
                    TargetSeverity: target.Severity,
                    TargetProductName: prodName,
                    TargetComponentName: compName,
                    RelationType: rel.RelationType,
                    SimilarityScore: rel.SimilarityScore,
                    MatchedFactors: rel.MatchedFactors,
                    CreatedBy: rel.CreatedBy,
                    CreatedByName: createdByName,
                    CreatedAt: rel.CreatedAt
                ));
            }
        }

        // Cálculo de Insight Agregado (BR-048: M >= 3 para validade estatística confiável)
        AggregatedInsightDto? insight = null;
        var resolvedSimilarCases = similarDtos
            .Where(s => string.Equals(s.TargetStatus, "Resolved", StringComparison.OrdinalIgnoreCase))
            .ToList();

        int m = resolvedSimilarCases.Count;
        if (m >= 3)
        {
            var resolvedIds = resolvedSimilarCases.Select(s => s.TargetCaseId).ToList();

            // 1. Tenta identificar passo investigativo bem-sucedido frequente
            var steps = await _caseRelationRepository.GetSuccessfulDiagnosticStepsForCasesAsync(resolvedIds, ct);
            var bestStepGroup = steps
                .Where(s => !string.IsNullOrWhiteSpace(s.Title))
                .GroupBy(s => s.Title.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (bestStepGroup != null && bestStepGroup.Count() >= 2)
            {
                int n = bestStepGroup.Count();
                string actionName = bestStepGroup.Key;
                insight = new AggregatedInsightDto(
                    ActionOrComponent: actionName,
                    SampleCount: m,
                    SuccessCount: n,
                    Text: $"{n} de {m} casos semelhantes foram resolvidos verificando/agindo sobre: {actionName}"
                );
            }
            else
            {
                // 2. Tenta identificar ação nas resoluções
                var resolutions = await _caseRelationRepository.GetResolutionsForCasesAsync(resolvedIds, ct);
                var bestResGroup = resolutions
                    .Where(r => !string.IsNullOrWhiteSpace(r.ResolutionSummary))
                    .GroupBy(r => r.ResolutionSummary.Trim(), StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (bestResGroup != null && bestResGroup.Count() >= 2)
                {
                    int n = bestResGroup.Count();
                    string actionName = bestResGroup.Key;
                    insight = new AggregatedInsightDto(
                        ActionOrComponent: actionName,
                        SampleCount: m,
                        SuccessCount: n,
                        Text: $"{n} de {m} casos semelhantes foram resolvidos verificando/agindo sobre: {actionName}"
                    );
                }
                else
                {
                    // 3. Fallback para o componente mais frequente associado aos casos resolvidos
                    var bestCompGroup = resolvedSimilarCases
                        .Where(s => !string.IsNullOrWhiteSpace(s.TargetComponentName))
                        .GroupBy(s => s.TargetComponentName!.Trim(), StringComparer.OrdinalIgnoreCase)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault();

                    if (bestCompGroup != null && bestCompGroup.Count() >= 2)
                    {
                        int n = bestCompGroup.Count();
                        string compName = bestCompGroup.Key;
                        insight = new AggregatedInsightDto(
                            ActionOrComponent: compName,
                            SampleCount: m,
                            SuccessCount: n,
                            Text: $"{n} de {m} casos semelhantes foram resolvidos no componente: {compName}"
                        );
                    }
                }
            }
        }

        return new CaseRelationsOverviewDto(
            CaseId: caseId,
            SimilarCases: similarDtos.OrderByDescending(s => s.SimilarityScore ?? 0).ToList(),
            ManualRelations: manualDtos.OrderByDescending(m => m.CreatedAt).ToList(),
            Insight: insight
        );
    }

    public async Task<long> CreateManualRelationAsync(CreateCaseRelationCommand command, long userId, CancellationToken ct = default)
    {
        if (userId <= 0)
            throw new ArgumentException("Usuário inválido para criação de relação.", nameof(userId));

        var sourceCase = await _caseRepository.GetByIdAsync(command.SourceCaseId, ct);
        if (sourceCase == null)
            throw new KeyNotFoundException($"Caso de origem #{command.SourceCaseId} não foi encontrado.");

        Case? targetCase = null;
        if (command.TargetCaseId.HasValue && command.TargetCaseId.Value > 0)
        {
            targetCase = await _caseRepository.GetByIdAsync(command.TargetCaseId.Value, ct);
        }
        else if (command.TargetCaseNumber.HasValue && command.TargetCaseNumber.Value > 0)
        {
            targetCase = await _caseRepository.GetByCaseNumberAsync(command.TargetCaseNumber.Value, ct);
        }

        if (targetCase == null)
            throw new KeyNotFoundException("Caso de destino não foi encontrado.");

        if (sourceCase.Id == targetCase.Id)
            throw new InvalidOperationException("Não é permitido relacionar um caso a ele próprio.");

        var relType = string.IsNullOrWhiteSpace(command.RelationType) ? "Duplicate" : command.RelationType.Trim();
        if (relType == "Similar")
            throw new InvalidOperationException("O tipo de relação 'Similar' é computado deterministicamente pelo sistema e não pode ser asserido manualmente.");

        // Verifica se relação manual já existe
        var existing = await _caseRelationRepository.GetRelationsByCaseIdAsync(sourceCase.Id, ct);
        bool alreadyExists = existing.Any(r => r.TargetCaseId == targetCase.Id && string.Equals(r.RelationType, relType, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
            throw new InvalidOperationException($"Já existe um relacionamento '{relType}' entre estes casos.");

        var relation = new CaseRelation(
            sourceCaseId: sourceCase.Id,
            targetCaseId: targetCase.Id,
            relationType: Enum.Parse<CaseRelationType>(relType, true),
            similarityScore: null,
            matchedFactors: new[] { $"Asserção manual de {relType}" },
            createdBy: userId
        );

        long relId = await _caseRelationRepository.AddManualRelationAsync(relation, ct);

        // Auditoria obrigatória (BR-004 e especificação da Fase 8)
        var audit = new AuditEvent(
            action: "CaseRelationCreated",
            entityType: "CaseRelation",
            entityId: relId.ToString(),
            actorUserId: userId,
            metadataJson: $"{{\"sourceCaseId\":{sourceCase.Id},\"targetCaseId\":{targetCase.Id},\"relationType\":\"{relType}\"}}"
        );
        await _auditEventRepository.AddAsync(audit, ct);

        return relId;
    }

    private static HashSet<string> ExtractSignificantWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new HashSet<string>();

        var words = Regex.Matches(text, @"\b[A-Za-z0-9_]{3,}\b")
            .Select(m => m.Value.ToLowerInvariant())
            .Where(w => !Stopwords.Contains(w));

        return new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
    }

    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "que", "para", "com", "não", "uma", "por", "mais", "dos", "como", "mas",
        "foi", "ao", "ele", "das", "tem", "à", "seu", "sua", "ou", "ser",
        "quando", "muito", "há", "nos", "já", "está", "eu", "também", "só",
        "pelo", "pela", "até", "isso", "ela", "entre", "era", "depois", "sem",
        "mesmo", "aos", "ter", "seus", "quem", "nas", "me", "esse", "eles",
        "estão", "você", "tinha", "foram", "essa", "num", "nem", "suas", "meu",
        "às", "minha", "têm", "numa", "pelos", "elas", "havia", "seja", "qual",
        "será", "nós", "tenho", "lhe", "deles", "essas", "esses", "pelas", "este",
        "fosse", "dele", "tu", "te", "vocês", "vos", "lhes", "meus", "minhas",
        "teu", "tua", "teus", "tuas", "nosso", "nossa", "nossos", "nossas", "dela",
        "delas", "esta", "estes", "estas", "aquele", "aquela", "aqueles", "aquelas",
        "isto", "aquilo", "estou", "está", "estamos", "estão", "estive", "esteve",
        "estivemos", "estiveram", "estava", "estávamos", "estavam", "caso", "erro"
    };
}
