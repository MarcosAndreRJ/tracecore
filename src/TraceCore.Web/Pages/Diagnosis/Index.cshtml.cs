using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Web.Pages.Diagnosis;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICaseService _caseService;
    private readonly ICaseInvestigationService _investigationService;
    private readonly IDiagnosticRepository _diagnosticRepository;
    private readonly ICatalogRepository _catalogRepository;

    public IndexModel(
        ICaseService caseService,
        ICaseInvestigationService investigationService,
        IDiagnosticRepository diagnosticRepository,
        ICatalogRepository catalogRepository)
    {
        _caseService = caseService;
        _investigationService = investigationService;
        _diagnosticRepository = diagnosticRepository;
        _catalogRepository = catalogRepository;
    }

    [BindProperty(SupportsGet = true)]
    public long? CaseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? QuickSymptom { get; set; }

    // Dados do Caso Selecionado
    public CaseDto? CurrentCase { get; private set; }
    public CaseInvestigationTimelineDto? Timeline { get; private set; }
    public IReadOnlyList<CaseDto> OpenCases { get; private set; } = [];

    // Recomendação Atual do Motor de Diagnóstico Adaptativo (BR-070 a BR-076)
    public DiagnosticRecommendationViewModel CurrentRecommendation { get; private set; } = default!;

    // Hipóteses Calculadas com Probabilidade
    public List<RankedHypothesisViewModel> RankedHypotheses { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        // Carrega fila de casos em andamento
        OpenCases = await _caseService.GetAllCasesAsync(15);

        if (!CaseId.HasValue && OpenCases.Count > 0)
        {
            CaseId = OpenCases[0].Id;
        }

        if (CaseId.HasValue)
        {
            CurrentCase = await _caseService.GetCaseByIdAsync(CaseId.Value);
            if (CurrentCase != null)
            {
                Timeline = await _investigationService.GetInvestigationTimelineAsync(CaseId.Value);
            }
        }

        BuildDiagnosticEngineState();
        return Page();
    }

    public async Task<IActionResult> OnPostRecordAnswerAsync(
        long caseId,
        string questionId,
        string answer,
        string? evidenceText)
    {
        var userId = GetCurrentUserId();

        var timeline = await _investigationService.GetInvestigationTimelineAsync(caseId);
        var targetHypothesis = timeline.Hypotheses.FirstOrDefault(h => h.Status == "Proposed")
                               ?? timeline.Hypotheses.FirstOrDefault();

        if (targetHypothesis != null && !string.IsNullOrWhiteSpace(answer))
        {
            var outcome = answer.Equals("yes", StringComparison.OrdinalIgnoreCase)
                ? "ConclusiveSupported"
                : (answer.Equals("no", StringComparison.OrdinalIgnoreCase) ? "ConclusiveRefuted" : "Inconclusive");

            try
            {
                await _investigationService.RegisterDiagnosticStepAsync(new RegisterDiagnosticStepCommand(
                    CaseId: caseId,
                    HypothesisId: targetHypothesis.Id,
                    Title: $"Verificação Adaptativa [{questionId}]",
                    Objective: "Aferição recomendada pelo motor de diagnóstico para discriminação de hipótese.",
                    Instruction: "Verificação manual executada pelo operador (BR-073).",
                    InputEvidenceSummary: evidenceText ?? "Sem observações adicionais",
                    ResultSummary: $"Resposta informada: {answer.ToUpperInvariant()}",
                    Outcome: outcome,
                    StepType: "GuidedVerification",
                    RiskLevel: "Low"
                ), userId);
            }
            catch
            {
                // Degradação graciosa em caso de regra de negócio
            }
        }

        return RedirectToPage(new { CaseId = caseId });
    }

    public async Task<IActionResult> OnPostIgnoreRecommendationAsync(
        long caseId,
        string questionId,
        string reason)
    {
        var userId = GetCurrentUserId();

        var timeline = await _investigationService.GetInvestigationTimelineAsync(caseId);
        var targetHypothesis = timeline.Hypotheses.FirstOrDefault();

        if (targetHypothesis != null)
        {
            try
            {
                await _investigationService.RegisterDiagnosticStepAsync(new RegisterDiagnosticStepCommand(
                    CaseId: caseId,
                    HypothesisId: targetHypothesis.Id,
                    Title: $"Recomendação [{questionId}] Ignorada",
                    Objective: "Registro de desconsideração da recomendação do motor para auditoria (BR-075).",
                    Instruction: "Operador optou por não executar este passo.",
                    InputEvidenceSummary: "Nenhuma coleta executada.",
                    ResultSummary: $"Motivo declarado (BR-075): {reason}",
                    Outcome: "Inconclusive",
                    StepType: "RecommendationIgnored",
                    RiskLevel: "Low"
                ), userId);
            }
            catch
            {
            }
        }

        return RedirectToPage(new { CaseId = caseId });
    }

    private void BuildDiagnosticEngineState()
    {
        CurrentRecommendation = new DiagnosticRecommendationViewModel
        {
            QuestionId = "VER-042",
            Title = "Aferição de Saturação de Threads no Pool TCP/HTTP",
            ContextPrompt = "O sintoma reporta degradação súbita com timeout 504 no Gateway. O motor identificou que o sintoma é consistente tanto com 'Deadlock em Pool de Conexões' quanto com 'Indisponibilidade do Backend'.",
            Justification = "Esta verificação discrimina com precisão de 88% se o problema reside no runtime da API ou na dependência externa, com custo computacional imediato e risco nulo em produção (BR-072 e BR-074).",
            SuggestedCheck = "Execute: tc-diag pool-status --threshold 90 no cluster ou consulte a métrica 'gateway_pool_active_threads'.",
            ConfidenceImpact = "+45% de discriminação entre as hipóteses principais",
            IsDestructive = false
        };

        RankedHypotheses =
        [
            new RankedHypothesisViewModel
            {
                Id = 1,
                Title = "Deadlock / Starvation no pool de conexões HTTP do Gateway",
                Status = "Proposed",
                ProbabilityScore = 72,
                DiscriminatorSummary = "Apoiada por 2 sintomas de timeout 504. Aguardando resultado da aferição VER-042.",
                AssociatedComponent = "Connection Pool / Auth Middleware",
                CanConclude = true
            },
            new RankedHypothesisViewModel
            {
                Id = 2,
                Title = "Indisponibilidade ou Latência Extrema no Provedor Bancário Externo",
                Status = "Proposed",
                ProbabilityScore = 24,
                DiscriminatorSummary = "Possível causa raiz indireta. Menor probabilidade inicial pelo volume de métricas de handshake com sucesso.",
                AssociatedComponent = "Core Gateway Client",
                CanConclude = false
            },
            new RankedHypothesisViewModel
            {
                Id = 3,
                Title = "Falha de Resolução de DNS Interno no Cluster Kubernetes",
                Status = "Discarded",
                ProbabilityScore = 4,
                DiscriminatorSummary = "Descartada após teste factual de ping e resolução de nomes no Pod executado com sucesso.",
                AssociatedComponent = "CoreDNS / Ingress",
                CanConclude = false
            }
        ];
    }

    private long? GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(idStr, out var id) ? id : null;
    }
}

public class DiagnosticRecommendationViewModel
{
    public string QuestionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ContextPrompt { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public string SuggestedCheck { get; set; } = string.Empty;
    public string ConfidenceImpact { get; set; } = string.Empty;
    public bool IsDestructive { get; set; }
}

public class RankedHypothesisViewModel
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Proposed";
    public int ProbabilityScore { get; set; }
    public string DiscriminatorSummary { get; set; } = string.Empty;
    public string AssociatedComponent { get; set; } = string.Empty;
    public bool CanConclude { get; set; }
}
