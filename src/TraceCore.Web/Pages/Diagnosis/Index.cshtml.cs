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
using TraceCore.Application.Exceptions;

namespace TraceCore.Web.Pages.Diagnosis;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICaseService _caseService;
    private readonly ICaseInvestigationService _investigationService;
    private readonly IDiagnosticEngineService _engineService;
    private readonly IAuthorizationService _authorizationService;

    public IndexModel(
        ICaseService caseService,
        ICaseInvestigationService investigationService,
        IDiagnosticEngineService engineService,
        IAuthorizationService authorizationService)
    {
        _caseService = caseService;
        _investigationService = investigationService;
        _engineService = engineService;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public long? CaseId { get; set; }

    public CaseDto? CurrentCase { get; private set; }
    public CaseInvestigationTimelineDto? Timeline { get; private set; }
    public IReadOnlyList<CaseDto> OpenCases { get; private set; } = [];

    public DiagnosticEngineStateDto EngineState { get; private set; } = default!;
    public DiagnosticFlowDto? SuggestedFlow { get; private set; }
    public bool CanConfigureFlows { get; private set; }

    // Testes e Validações (evidências do tipo DiagnosticTest registradas para o caso)
    public IReadOnlyList<CaseEvidenceDto> TestEvidences { get; private set; } = [];

    [BindProperty]
    public string? TestDescriptionInput { get; set; }
    [BindProperty]
    public long? TestHypothesisIdInput { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var authConfig = await _authorizationService.AuthorizeAsync(User, "diagnostico.configurar");
        CanConfigureFlows = authConfig.Succeeded;

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
                EngineState = await _engineService.GetEngineStateAsync(CaseId.Value);

                var evidences = await _investigationService.GetEvidencesByCaseIdAsync(CaseId.Value);
                TestEvidences = evidences
                    .Where(e => e.EvidenceType == "DiagnosticTest")
                    .OrderByDescending(e => e.CreatedAt)
                    .ToList();

                if (EngineState.ActiveFlow == null)
                {
                    SuggestedFlow = await _engineService.SuggestFlowForCaseAsync(CaseId.Value);
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostStartFlowAsync(long caseId, long flowId)
    {
        var userId = GetCurrentUserId() ?? 1L;

        try
        {
            await _engineService.StartFlowAsync(caseId, flowId, userId);
            StatusMessage = "Fluxo de diagnóstico guiado iniciado com sucesso. Hipóteses candidatas vinculadas à investigação.";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao iniciar fluxo: {ex.Message}";
        }

        return RedirectToPage(new { CaseId = caseId });
    }

    public async Task<IActionResult> OnPostRecordAnswerAsync(
        long caseId,
        long checkId,
        long optionId,
        string? evidenceText)
    {
        var userId = GetCurrentUserId() ?? 1L;

        try
        {
            await _engineService.AnswerCheckAsync(caseId, checkId, optionId, evidenceText, userId);
            StatusMessage = "Resposta registrada com sucesso na timeline e impactos aplicados às hipóteses!";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar resposta: {ex.Message}";
        }

        return RedirectToPage(new { CaseId = caseId });
    }

    public async Task<IActionResult> OnPostIgnoreRecommendationAsync(
        long caseId,
        long checkId,
        string reason)
    {
        var userId = GetCurrentUserId() ?? 1L;

        try
        {
            await _engineService.IgnoreRecommendationAsync(caseId, checkId, reason, userId);
            StatusMessage = "Recomendação desconsiderada e justificativa auditada conforme BR-075.";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio (BR-075): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar desconsideração: {ex.Message}";
        }

        return RedirectToPage(new { CaseId = caseId });
    }

    public async Task<IActionResult> OnPostRecordTestAsync(long caseId)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "caso.diagnosticar");
        if (!auth.Succeeded) return Forbid();

        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Challenge();

        if (string.IsNullOrWhiteSpace(TestDescriptionInput))
        {
            ErrorMessage = "A descrição do teste/validação é obrigatória.";
            return RedirectToPage(new { CaseId = caseId });
        }

        try
        {
            var relations = new List<HypothesisEvidenceRelationInputDto>();
            if (TestHypothesisIdInput.HasValue && TestHypothesisIdInput.Value > 0)
            {
                relations.Add(new HypothesisEvidenceRelationInputDto(
                    TestHypothesisIdInput.Value,
                    "Inconclusive",
                    null
                ));
            }

            await _investigationService.RecordEvidenceAsync(new RecordEvidenceCommand(
                CaseId: caseId,
                EvidenceType: "DiagnosticTest",
                Description: TestDescriptionInput.Trim(),
                HypothesisRelations: relations
            ), userId.Value);

            StatusMessage = "Teste/validação registrado com sucesso na Central de Diagnóstico Guiado.";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar teste: {ex.Message}";
        }

        return RedirectToPage(new { CaseId = caseId });
    }

    private long? GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(idStr, out var id) ? id : null;
    }
}
