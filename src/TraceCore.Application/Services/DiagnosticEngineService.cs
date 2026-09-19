using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;
using TraceCore.Application.Exceptions;

namespace TraceCore.Application.Services;

public class DiagnosticEngineService : IDiagnosticEngineService
{
    private readonly IDiagnosticFlowRepository _flowRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly ICaseInvestigationService _investigationService;
    private readonly IDiagnosticRepository _diagnosticRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IAuditEventRepository _auditEventRepository;
    private readonly IIntegrationHealthCheckService? _healthCheckService;

    public DiagnosticEngineService(
        IDiagnosticFlowRepository flowRepository,
        ICaseRepository caseRepository,
        ICaseInvestigationService investigationService,
        IDiagnosticRepository diagnosticRepository,
        ICatalogRepository catalogRepository,
        IAuditEventRepository auditEventRepository,
        IIntegrationHealthCheckService? healthCheckService = null)
    {
        _flowRepository = flowRepository;
        _caseRepository = caseRepository;
        _investigationService = investigationService;
        _diagnosticRepository = diagnosticRepository;
        _catalogRepository = catalogRepository;
        _auditEventRepository = auditEventRepository;
        _healthCheckService = healthCheckService;
    }

    public async Task<IReadOnlyList<DiagnosticFlowDto>> GetAllFlowsAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var flows = await _flowRepository.GetAllFlowsAsync(activeOnly, ct);
        var dtos = new List<DiagnosticFlowDto>();

        foreach (var f in flows)
        {
            var hyps = await _flowRepository.GetHypothesesByFlowIdAsync(f.Id, ct);
            var checks = await _flowRepository.GetChecksByFlowIdAsync(f.Id, ct);

            dtos.Add(new DiagnosticFlowDto(
                f.Id,
                f.Code,
                f.Name,
                f.Description,
                f.EntryKeywords,
                f.Status,
                hyps.Count,
                checks.Count,
                f.CreatedAt
            ));
        }

        return dtos;
    }

    public async Task<DiagnosticFlowDto?> SuggestFlowForCaseAsync(long caseId, CancellationToken ct = default)
    {
        var @case = await _caseRepository.GetByIdAsync(caseId, ct);
        if (@case == null) return null;

        var flows = await GetAllFlowsAsync(activeOnly: true, ct);
        if (flows.Count == 0) return null;

        // Monta texto consolidado do caso para matching de palavras-chave
        var caseText = $"{@case.OriginalReport} {@case.NormalizedSummary} {@case.ObservedBehavior} {@case.ErrorCode}".ToLowerInvariant();

        DiagnosticFlowDto? bestFlow = null;
        int bestMatchCount = 0;

        foreach (var flow in flows)
        {
            var keywords = flow.EntryKeywords
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim().ToLowerInvariant())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .ToList();

            int matches = 0;
            foreach (var kw in keywords)
            {
                if (caseText.Contains(kw))
                {
                    matches++;
                }
            }

            if (matches > bestMatchCount)
            {
                bestMatchCount = matches;
                bestFlow = flow;
            }
        }

        return bestMatchCount > 0 ? bestFlow : null;
    }

    public async Task StartFlowAsync(long caseId, long flowId, long userId, CancellationToken ct = default)
    {
        var @case = await _caseRepository.GetByIdAsync(caseId, ct);
        if (@case == null)
            throw new EntityNotFoundException("Caso", caseId);

        if (@case.Status == "Resolved")
            throw new BusinessRuleValidationException("BR-070", "Não é permitido iniciar diagnóstico guiado em um caso já encerrado/resolvido.");

        var flow = await _flowRepository.GetFlowByIdAsync(flowId, ct);
        if (flow == null)
            throw new EntityNotFoundException("Fluxo de Diagnóstico", flowId);

        var timeline = await _investigationService.GetInvestigationTimelineAsync(caseId, ct);
        var existingTitles = timeline.Hypotheses.Select(h => h.Title.Trim().ToLowerInvariant()).ToHashSet();

        // Cria as hipóteses candidatas do flow como hipóteses reais do caso (SourceType = "Guided")
        foreach (var hyp in flow.CandidateHypotheses)
        {
            if (!existingTitles.Contains(hyp.Title.Trim().ToLowerInvariant()))
            {
                await _investigationService.RegisterHypothesisAsync(new RegisterHypothesisCommand(
                    CaseId: caseId,
                    Title: hyp.Title,
                    Description: hyp.Description,
                    ComponentId: hyp.AssociatedComponentId,
                    SourceType: "Guided"
                ), userId, ct);
            }
        }
    }

    public async Task<DiagnosticEngineStateDto> GetEngineStateAsync(long caseId, CancellationToken ct = default)
    {
        var @case = await _caseRepository.GetByIdAsync(caseId, ct);
        if (@case == null)
            throw new EntityNotFoundException("Caso", caseId);

        var timeline = await _investigationService.GetInvestigationTimelineAsync(caseId, ct);
        var availableFlows = (await GetAllFlowsAsync(activeOnly: true, ct)).ToList();

        // 1. Identifica se há um fluxo ativo semeado no caso
        DiagnosticFlow? activeFlow = null;
        var guidedHypotheses = timeline.Hypotheses.Where(h => string.Equals(h.SourceType, "Guided", StringComparison.OrdinalIgnoreCase)).ToList();

        if (guidedHypotheses.Count > 0)
        {
            // Tenta casar títulos das hipóteses com os flows existentes
            foreach (var f in availableFlows)
            {
                var flowEntity = await _flowRepository.GetFlowByIdAsync(f.Id, ct);
                if (flowEntity != null)
                {
                    var flowTitles = flowEntity.CandidateHypotheses.Select(ch => ch.Title.Trim().ToLowerInvariant()).ToHashSet();
                    if (guidedHypotheses.Any(gh => flowTitles.Contains(gh.Title.Trim().ToLowerInvariant())))
                    {
                        activeFlow = flowEntity;
                        break;
                    }
                }
            }
        }

        DiagnosticFlowDto? activeFlowDto = activeFlow != null
            ? new DiagnosticFlowDto(activeFlow.Id, activeFlow.Code, activeFlow.Name, activeFlow.Description, activeFlow.EntryKeywords, activeFlow.Status, activeFlow.CandidateHypotheses.Count, activeFlow.Checks.Count, activeFlow.CreatedAt)
            : null;

        // 2. Calcula pesos acumulados e ranking investigativo das hipóteses
        // Prioridade investigativa (P-006: nunca tratada como probabilidade científica ou verdade matemática)
        var rankedHypotheses = new List<InvestigativeRankedHypothesisDto>();
        var components = await _catalogRepository.GetAllComponentsAsync(ct: ct);
        var compLookup = components.ToDictionary(c => c.Id, c => c.Name);

        // Mapeia histórico de respostas guiadas e verificações automáticas
        var guidedSteps = timeline.Steps
            .Where(s => string.Equals(s.StepType, DiagnosticStepTypes.GuidedQuestion, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s.StepType, DiagnosticStepTypes.AutomatedCheck, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var checks = activeFlow?.Checks ?? new List<DiagnosticCheck>();
        var checkOptions = checks.SelectMany(c => c.Options).ToList();
        var allImpacts = checkOptions.SelectMany(o => o.Impacts).ToList();

        // Contabiliza impactos ocorridos
        var favorsMap = new Dictionary<long, decimal>();
        var discardsMap = new Dictionary<long, decimal>();

        foreach (var h in timeline.Hypotheses)
        {
            favorsMap[h.Id] = 0;
            discardsMap[h.Id] = 0;

            if (activeFlow != null)
            {
                var candidate = activeFlow.CandidateHypotheses.FirstOrDefault(ch => string.Equals(ch.Title.Trim(), h.Title.Trim(), StringComparison.OrdinalIgnoreCase));
                if (candidate != null)
                {
                    foreach (var step in guidedSteps)
                    {
                        var matchingOption = checkOptions.FirstOrDefault(o => step.ResultSummary.Contains(o.OptionText));
                        if (matchingOption != null)
                        {
                            var impacts = matchingOption.Impacts.Where(i => i.FlowHypothesisId == candidate.Id);
                            foreach (var imp in impacts)
                            {
                                if (string.Equals(imp.ImpactType, "Favors", StringComparison.OrdinalIgnoreCase))
                                    favorsMap[h.Id] += imp.Weight;
                                else if (string.Equals(imp.ImpactType, "Discards", StringComparison.OrdinalIgnoreCase))
                                    discardsMap[h.Id] += imp.Weight;
                            }
                        }
                    }
                }
            }
        }

        // Ordenação por relevância investigativa:
        // 1º: Supported
        // 2º: Proposed ordenadas por saldo (Favors - Discards) descrescente
        // 3º: Discarded
        var orderedHypotheses = timeline.Hypotheses
            .OrderBy(h => h.Status == "Supported" ? 0 : (h.Status == "Proposed" ? 1 : 2))
            .ThenByDescending(h => favorsMap[h.Id] - discardsMap[h.Id])
            .ToList();

        int rank = 1;
        foreach (var h in orderedHypotheses)
        {
            string label = h.Status == "Supported" ? "Confirmada" : (h.Status == "Discarded" ? "Descartada" : (rank == 1 ? "Alta" : (rank == 2 ? "Média" : "Baixa")));
            string? compName = h.ComponentId.HasValue && compLookup.TryGetValue(h.ComponentId.Value, out var cn) ? cn : null;

            rankedHypotheses.Add(new InvestigativeRankedHypothesisDto(
                Id: h.Id,
                Title: h.Title,
                Status: h.Status,
                PriorityRank: rank++,
                PriorityLabel: label,
                AssociatedComponent: compName,
                FavorsWeight: favorsMap[h.Id],
                DiscardsWeight: discardsMap[h.Id],
                Justification: h.Justification
            ));
        }

        // 3. Heurística de Próxima Recomendação (doc 05 §7)
        DiagnosticRecommendationDto? recommendation = null;
        int answeredCount = guidedSteps.Count + timeline.Steps.Count(s => string.Equals(s.StepType, DiagnosticStepTypes.RecommendationIgnored, StringComparison.OrdinalIgnoreCase));

        if (activeFlow != null)
        {
            // Conjunto de códigos de checks já respondidos ou ignorados
            var processedCheckCodes = timeline.Steps
                .Where(s => s.Title.Contains('[') && s.Title.Contains(']'))
                .Select(s =>
                {
                    int start = s.Title.IndexOf('[');
                    int end = s.Title.IndexOf(']', start + 1);
                    return (start >= 0 && end > start) ? s.Title.Substring(start + 1, end - start - 1) : string.Empty;
                })
                .Where(c => !string.IsNullOrEmpty(c))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var candidateChecks = activeFlow.Checks
                .Where(c => !processedCheckCodes.Contains(c.Code))
                .ToList();

            // Pula perguntas redundantes cuja resposta já esteja nos campos do caso (Anti-padrão §9)
            candidateChecks = candidateChecks.Where(c =>
            {
                if (!string.IsNullOrWhiteSpace(c.SkipConditionField))
                {
                    if (c.SkipConditionField.Equals("Cases.EnvironmentId", StringComparison.OrdinalIgnoreCase) && @case.EnvironmentId.HasValue) return false;
                    if (c.SkipConditionField.Equals("Cases.ProductVersionId", StringComparison.OrdinalIgnoreCase) && @case.ProductVersionId.HasValue) return false;
                    if (c.SkipConditionField.Equals("Cases.ErrorCode", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(@case.ErrorCode)) return false;
                }
                return true;
            }).ToList();

            // Calcula heurística para cada check restante:
            // prioridade = poder_discriminativo * confiabilidade / (custo + risco + 1)
            var activeHypothesisIds = timeline.Hypotheses
                .Where(h => h.Status == "Proposed")
                .Select(h =>
                {
                    var cand = activeFlow.CandidateHypotheses.FirstOrDefault(ch => string.Equals(ch.Title.Trim(), h.Title.Trim(), StringComparison.OrdinalIgnoreCase));
                    return cand?.Id ?? 0L;
                })
                .Where(id => id > 0)
                .ToHashSet();

            DiagnosticCheck? bestCheck = null;
            double bestScore = -1.0;
            string bestExplanation = string.Empty;

            foreach (var check in candidateChecks)
            {
                var impactedActiveHypIds = check.Options
                    .SelectMany(o => o.Impacts)
                    .Where(i => activeHypothesisIds.Contains(i.FlowHypothesisId))
                    .Select(i => i.FlowHypothesisId)
                    .Distinct()
                    .ToList();

                double poderDiscriminativo = impactedActiveHypIds.Count;
                double confiabilidade = 1.0; // TODO: calibrar por dados históricos futuros (doc 05 §7)
                int riskWeight = check.RiskLevel switch
                {
                    "High" => 3,
                    "Medium" => 2,
                    _ => 1
                };

                double score = (poderDiscriminativo * confiabilidade) / (check.Cost + riskWeight + 1);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCheck = check;

                    // Explicação do porquê da recomendação (BR-074)
                    var favorsHypTitles = check.Options
                        .SelectMany(o => o.Impacts)
                        .Where(i => i.ImpactType == "Favors")
                        .Select(i => activeFlow.CandidateHypotheses.FirstOrDefault(ch => ch.Id == i.FlowHypothesisId)?.Title)
                        .Where(t => t != null)
                        .Distinct()
                        .Take(2)
                        .ToList();

                    var discardsHypTitles = check.Options
                        .SelectMany(o => o.Impacts)
                        .Where(i => i.ImpactType == "Discards")
                        .Select(i => activeFlow.CandidateHypotheses.FirstOrDefault(ch => ch.Id == i.FlowHypothesisId)?.Title)
                        .Where(t => t != null)
                        .Distinct()
                        .Take(2)
                        .ToList();

                    var parts = new List<string>();
                    if (favorsHypTitles.Count > 0)
                        parts.Add($"Favorece: {string.Join(", ", favorsHypTitles)}");
                    if (discardsHypTitles.Count > 0)
                        parts.Add($"Descarta: {string.Join(", ", discardsHypTitles)}");

                    string exp = parts.Count > 0
                        ? string.Join(" • ", parts) + $" (Custo: {check.Cost}, Risco: {check.RiskLevel})"
                        : $"Discriminação orientada de hipóteses com risco {check.RiskLevel} e custo computacional {check.Cost}.";

                    bestExplanation = exp;
                }
            }

            if (bestCheck != null)
            {
                // BR-073 (Fase 15): Verificação automática sem intervenção humana quando IntegrationId estiver configurado
                if (string.Equals(bestCheck.CheckType, "AutomatedCheck", StringComparison.OrdinalIgnoreCase) &&
                    bestCheck.IntegrationId.HasValue &&
                    _healthCheckService != null)
                {
                    var run = await _healthCheckService.ExecuteHealthCheckAsync(bestCheck.IntegrationId.Value, ct);

                    var stepOutcome = string.Equals(run.Status, "Success", StringComparison.OrdinalIgnoreCase)
                        ? DiagnosticStepOutcome.Worked
                        : DiagnosticStepOutcome.DidNotWork;

                    long? targetHypId = null;
                    if (activeFlow != null)
                    {
                        var firstHyp = activeFlow.CandidateHypotheses.FirstOrDefault();
                        if (firstHyp != null)
                        {
                            var caseHyp = timeline.Hypotheses.FirstOrDefault(h => string.Equals(h.Title.Trim(), firstHyp.Title.Trim(), StringComparison.OrdinalIgnoreCase));
                            targetHypId = caseHyp?.Id;
                        }
                    }
                    targetHypId ??= timeline.Hypotheses.FirstOrDefault(h => h.Status == "Proposed")?.Id ?? timeline.Hypotheses.FirstOrDefault()?.Id;
                    long actorUserId = @case.CurrentOwnerUserId ?? @case.CreatedBy ?? 1;

                    await _investigationService.RegisterDiagnosticStepAsync(new RegisterDiagnosticStepCommand(
                        CaseId: caseId,
                        HypothesisId: targetHypId,
                        Title: $"Verificação Automática [{bestCheck.Code}]: {bestCheck.Title}",
                        Objective: bestCheck.QuestionText,
                        Instruction: $"Verificação automática de saúde executada sem intervenção humana (BR-073).",
                        InputEvidenceSummary: $"Integração #{bestCheck.IntegrationId.Value}",
                        ResultSummary: $"Status da execução: {run.Status}. {(string.IsNullOrWhiteSpace(run.ErrorMessage) ? $"Código HTTP/porta {run.RecordsProcessed ?? 200}" : run.ErrorMessage)}",
                        Outcome: stepOutcome.ToString(),
                        StepType: DiagnosticStepTypes.AutomatedCheck,
                        RiskLevel: bestCheck.RiskLevel
                    ), actorUserId, ct);

                    if (bestCheck.Options.Count > 0)
                    {
                        var chosenOpt = stepOutcome == DiagnosticStepOutcome.Worked
                            ? bestCheck.Options.FirstOrDefault()
                            : (bestCheck.Options.Count > 1 ? bestCheck.Options[1] : bestCheck.Options.FirstOrDefault());

                        if (chosenOpt != null)
                        {
                            foreach (var impact in chosenOpt.Impacts)
                            {
                                var candHyp = activeFlow?.CandidateHypotheses.FirstOrDefault(ch => ch.Id == impact.FlowHypothesisId);
                                if (candHyp != null)
                                {
                                    var caseHyp = timeline.Hypotheses.FirstOrDefault(h => string.Equals(h.Title.Trim(), candHyp.Title.Trim(), StringComparison.OrdinalIgnoreCase));
                                    if (caseHyp != null && caseHyp.Status == "Proposed" && impact.Weight >= 2.0m)
                                    {
                                        var newStatus = string.Equals(impact.ImpactType, "Favors", StringComparison.OrdinalIgnoreCase)
                                            ? HypothesisStatus.Supported
                                            : HypothesisStatus.Discarded;

                                        await _investigationService.EvaluateHypothesisAsync(
                                            new EvaluateHypothesisCommand(
                                                HypothesisId: caseHyp.Id,
                                                NewStatus: newStatus.ToString(),
                                                Justification: $"Avaliação automática derivada de [{bestCheck.Code}] ({impact.ImpactType}, peso {impact.Weight}) via verificação de integração."
                                            ),
                                            actorUserId,
                                            ct);
                                    }
                                }
                            }
                        }
                    }

                    return await GetEngineStateAsync(caseId, ct);
                }

                // Fallback manual suave (quando não for AutomatedCheck ou não tiver IntegrationId configurado)
                recommendation = new DiagnosticRecommendationDto(
                    CheckId: bestCheck.Id,
                    CheckCode: bestCheck.Code,
                    Title: bestCheck.Title,
                    QuestionText: bestCheck.QuestionText,
                    Cost: bestCheck.Cost,
                    RiskLevel: bestCheck.RiskLevel,
                    HeuristicScore: Math.Round(bestScore, 2),
                    Explanation: bestExplanation,
                    Options: bestCheck.Options.Select(o => new DiagnosticCheckOptionDto(
                        o.Id,
                        o.CheckId,
                        o.OptionText,
                        o.OrderNo,
                        o.Impacts.Select(i => new DiagnosticCheckImpactDto(i.Id, i.CheckOptionId, i.FlowHypothesisId, i.ImpactType, i.Weight)).ToList()
                    )).ToList(),
                    SuggestEscalation: answeredCount >= 5 && timeline.Hypotheses.Count(h => h.Status == "Proposed") >= 2
                );
            }
        }

        bool suggestEscalation = answeredCount >= 5 && timeline.Hypotheses.Count(h => h.Status == "Proposed") >= 2;

        return new DiagnosticEngineStateDto(
            CaseId: caseId,
            ActiveFlow: activeFlowDto,
            Hypotheses: rankedHypotheses,
            CurrentRecommendation: recommendation,
            AnsweredChecksCount: answeredCount,
            SuggestEscalation: suggestEscalation,
            AvailableFlows: availableFlows
        );
    }

    public async Task AnswerCheckAsync(long caseId, long checkId, long optionId, string? evidenceText, long userId, CancellationToken ct = default)
    {
        var @case = await _caseRepository.GetByIdAsync(caseId, ct);
        if (@case == null)
            throw new EntityNotFoundException("Caso", caseId);

        var check = await _flowRepository.GetCheckByIdAsync(checkId, ct);
        if (check == null)
            throw new EntityNotFoundException("Verificação de Diagnóstico", checkId);

        var option = check.Options.FirstOrDefault(o => o.Id == optionId);
        if (option == null)
            throw new EntityNotFoundException("Opção de Verificação", optionId);

        var timeline = await _investigationService.GetInvestigationTimelineAsync(caseId, ct);
        var flow = await _flowRepository.GetFlowByIdAsync(check.FlowId, ct);

        // Mapeamento de resultado da ação do passo (DiagnosticStepOutcome)
        // Decisão de arquitetura:
        // - Opção apontando com Favors predominante: Worked (a verificação produziu sinal afirmativo)
        // - Opção com Discards predominante: DidNotWork (a verificação refutou o caminho)
        // - Caso ambíguo: Inconclusive
        var favorsSum = option.Impacts.Where(i => string.Equals(i.ImpactType, "Favors", StringComparison.OrdinalIgnoreCase)).Sum(i => i.Weight);
        var discardsSum = option.Impacts.Where(i => string.Equals(i.ImpactType, "Discards", StringComparison.OrdinalIgnoreCase)).Sum(i => i.Weight);

        var stepOutcome = (favorsSum > discardsSum)
            ? DiagnosticStepOutcome.Worked
            : ((discardsSum > favorsSum) ? DiagnosticStepOutcome.DidNotWork : DiagnosticStepOutcome.Inconclusive);

        // Encontra hipótese prioritária vinculada para associar ao passo
        long? targetHypId = null;
        if (option.Impacts.Count > 0 && flow != null)
        {
            var firstImpact = option.Impacts.First();
            var candHyp = flow.CandidateHypotheses.FirstOrDefault(ch => ch.Id == firstImpact.FlowHypothesisId);
            if (candHyp != null)
            {
                var caseHyp = timeline.Hypotheses.FirstOrDefault(h => string.Equals(h.Title.Trim(), candHyp.Title.Trim(), StringComparison.OrdinalIgnoreCase));
                targetHypId = caseHyp?.Id;
            }
        }
        targetHypId ??= timeline.Hypotheses.FirstOrDefault(h => h.Status == "Proposed")?.Id ?? timeline.Hypotheses.FirstOrDefault()?.Id;

        // 1. Grava o DiagnosticStep real
        await _investigationService.RegisterDiagnosticStepAsync(new RegisterDiagnosticStepCommand(
            CaseId: caseId,
            HypothesisId: targetHypId,
            Title: $"Verificação [{check.Code}]: {check.Title}",
            Objective: check.QuestionText,
            Instruction: "Verificação guiada pelo motor adaptativo respondida pelo operador (BR-073).",
            InputEvidenceSummary: string.IsNullOrWhiteSpace(evidenceText) ? "Sem evidência textual complementar" : evidenceText.Trim(),
            ResultSummary: $"Opção informada: {option.OptionText}",
            Outcome: stepOutcome.ToString(),
            StepType: DiagnosticStepTypes.GuidedQuestion,
            RiskLevel: check.RiskLevel
        ), userId, ct);

        // 2. Aplica os impactos configurados sobre as hipóteses ativas do caso
        if (flow != null)
        {
            foreach (var impact in option.Impacts)
            {
                var candHyp = flow.CandidateHypotheses.FirstOrDefault(ch => ch.Id == impact.FlowHypothesisId);
                if (candHyp != null)
                {
                    var caseHyp = timeline.Hypotheses.FirstOrDefault(h => string.Equals(h.Title.Trim(), candHyp.Title.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (caseHyp != null && caseHyp.Status == "Proposed")
                    {
                        // Limiar de transição configurado: peso >= 2.0 avalia formalmente
                        if (string.Equals(impact.ImpactType, "Favors", StringComparison.OrdinalIgnoreCase) && impact.Weight >= 2.0m)
                        {
                            await _investigationService.EvaluateHypothesisAsync(new EvaluateHypothesisCommand(
                                HypothesisId: caseHyp.Id,
                                NewStatus: nameof(HypothesisStatus.Supported),
                                Justification: $"Favorecida pela verificação [{check.Code}] com resposta '{option.OptionText}' (BR-072)."
                            ), userId, ct);
                        }
                        else if (string.Equals(impact.ImpactType, "Discards", StringComparison.OrdinalIgnoreCase) && impact.Weight >= 2.0m)
                        {
                            await _investigationService.EvaluateHypothesisAsync(new EvaluateHypothesisCommand(
                                HypothesisId: caseHyp.Id,
                                NewStatus: nameof(HypothesisStatus.Discarded),
                                Justification: $"Descartada pela verificação [{check.Code}] com resposta '{option.OptionText}' (BR-072)."
                            ), userId, ct);
                        }
                    }
                }
            }
        }
    }

    public async Task IgnoreRecommendationAsync(long caseId, long checkId, string reason, long userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessRuleValidationException("BR-075", "A justificativa para ignorar a recomendação do motor de diagnóstico é obrigatória.");
        }

        var @case = await _caseRepository.GetByIdAsync(caseId, ct);
        if (@case == null)
            throw new EntityNotFoundException("Caso", caseId);

        var check = await _flowRepository.GetCheckByIdAsync(checkId, ct);
        if (check == null)
            throw new EntityNotFoundException("Verificação de Diagnóstico", checkId);

        var timeline = await _investigationService.GetInvestigationTimelineAsync(caseId, ct);
        var targetHypId = timeline.Hypotheses.FirstOrDefault(h => h.Status == "Proposed")?.Id ?? timeline.Hypotheses.FirstOrDefault()?.Id;

        // Registra o passo com tipo RecommendationIgnored (BR-075)
        await _investigationService.RegisterDiagnosticStepAsync(new RegisterDiagnosticStepCommand(
            CaseId: caseId,
            HypothesisId: targetHypId,
            Title: $"Recomendação [{check.Code}] Ignorada",
            Objective: check.QuestionText,
            Instruction: "Operador optou por não executar este passo de verificação recomendado pelo motor (BR-075).",
            InputEvidenceSummary: "Nenhuma coleta executada.",
            ResultSummary: $"Motivo da desconsideração (BR-075): {reason.Trim()}",
            Outcome: DiagnosticStepOutcome.Inconclusive.ToString(),
            StepType: DiagnosticStepTypes.RecommendationIgnored,
            RiskLevel: check.RiskLevel
        ), userId, ct);
    }

    // Gestão e Administração do Grafo (Fase 9 / Bloco 5)
    public async Task<DiagnosticFlowDetailsDto?> GetFlowDetailsAsync(long flowId, CancellationToken ct = default)
    {
        var flow = await _flowRepository.GetFlowByIdAsync(flowId, ct);
        if (flow == null) return null;

        var hyps = await _flowRepository.GetHypothesesByFlowIdAsync(flow.Id, ct);
        var checks = await _flowRepository.GetChecksByFlowIdAsync(flow.Id, ct);
        var components = await _catalogRepository.GetAllComponentsAsync(ct: ct);
        var compLookup = components.ToDictionary(c => c.Id, c => c.Name);

        var hypDtos = hyps.Select(h => new DiagnosticFlowHypothesisDto(
            h.Id,
            h.FlowId,
            h.Title,
            h.Description,
            h.AssociatedComponentId,
            h.AssociatedComponentId.HasValue && compLookup.TryGetValue(h.AssociatedComponentId.Value, out var cn) ? cn : null
        )).ToList();

        var checkDtos = checks.Select(c => new DiagnosticCheckDto(
            c.Id,
            c.FlowId,
            c.Code,
            c.Title,
            c.QuestionText,
            c.CheckType,
            c.Cost,
            c.RiskLevel,
            c.SkipConditionField,
            c.Options.Select(o => new DiagnosticCheckOptionDto(
                o.Id,
                o.CheckId,
                o.OptionText,
                o.OrderNo,
                o.Impacts.Select(i => new DiagnosticCheckImpactDto(
                    i.Id,
                    i.CheckOptionId,
                    i.FlowHypothesisId,
                    i.ImpactType,
                    i.Weight,
                    hyps.FirstOrDefault(hyp => hyp.Id == i.FlowHypothesisId)?.Title
                )).ToList()
            )).ToList(),
            c.IntegrationId
        )).ToList();

        return new DiagnosticFlowDetailsDto(
            flow.Id,
            flow.Code,
            flow.Name,
            flow.Description,
            flow.EntryKeywords,
            flow.Status,
            hypDtos,
            checkDtos
        );
    }

    public async Task<long> CreateFlowAsync(CreateDiagnosticFlowCommand command, long userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
            throw new BusinessRuleValidationException("BR-070", "O código do fluxo é obrigatório.");
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new BusinessRuleValidationException("BR-070", "O nome do fluxo é obrigatório.");
        if (string.IsNullOrWhiteSpace(command.EntryKeywords))
            throw new BusinessRuleValidationException("BR-070", "Pelo menos uma palavra-chave de entrada é obrigatória.");

        var flow = new DiagnosticFlow(
            code: command.Code,
            name: command.Name,
            entryKeywords: command.EntryKeywords,
            description: command.Description,
            status: command.Status,
            createdBy: userId
        );

        long id = await _flowRepository.AddFlowAsync(flow, ct);

        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "DiagnosticFlowCreated",
            entityType: "DiagnosticFlow",
            entityId: id.ToString(),
            actorUserId: userId,
            metadataJson: $"{{\"code\":\"{flow.Code}\",\"name\":\"{flow.Name}\"}}"
        ), ct);

        return id;
    }

    public async Task<long> AddFlowHypothesisAsync(long flowId, string title, string? description, long? componentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessRuleValidationException("BR-023", "O título da hipótese é obrigatório.");

        var hyp = new DiagnosticFlowHypothesis(flowId, title, description, componentId);
        return await _flowRepository.AddHypothesisAsync(hyp, ct);
    }

    public async Task<long> AddCheckAsync(CreateDiagnosticCheckCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
            throw new BusinessRuleValidationException("BR-071", "O código do check é obrigatório.");
        if (string.IsNullOrWhiteSpace(command.Title))
            throw new BusinessRuleValidationException("BR-071", "O título do check é obrigatório.");
        if (string.IsNullOrWhiteSpace(command.QuestionText))
            throw new BusinessRuleValidationException("BR-071", "A pergunta do check é obrigatória.");

        var check = new DiagnosticCheck(
            flowId: command.FlowId,
            code: command.Code,
            title: command.Title,
            questionText: command.QuestionText,
            checkType: command.CheckType,
            cost: command.Cost,
            riskLevel: command.RiskLevel,
            skipConditionField: command.SkipConditionField
        );
        check.IntegrationId = command.IntegrationId;

        return await _flowRepository.AddCheckAsync(check, ct);
    }

    public async Task<long> AddCheckOptionWithImpactsAsync(long checkId, string optionText, int orderNo, List<(long HypothesisId, string ImpactType, decimal Weight)> impacts, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(optionText))
            throw new BusinessRuleValidationException("BR-071", "O texto da opção é obrigatório.");

        var option = new DiagnosticCheckOption(checkId, optionText, orderNo);
        long optId = await _flowRepository.AddCheckOptionAsync(option, ct);

        foreach (var (hypId, impactType, weight) in impacts)
        {
            var impact = new DiagnosticCheckImpact(optId, hypId, impactType, weight);
            await _flowRepository.AddCheckImpactAsync(impact, ct);
        }

        return optId;
    }

    public async Task DeleteFlowAsync(long flowId, CancellationToken ct = default)
    {
        await _flowRepository.DeleteFlowAsync(flowId, ct);
    }
}
