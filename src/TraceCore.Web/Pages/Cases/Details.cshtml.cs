using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;

namespace TraceCore.Web.Pages.Cases;

[Authorize(Policy = "caso.visualizar")]
public class DetailsModel : PageModel
{
    private readonly ICaseService _caseService;
    private readonly ICaseInvestigationService _investigationService;
    private readonly ICaseResolutionService _caseResolutionService;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IAuthorizationService _authorizationService;
    private readonly IKnowledgeService _knowledgeService;
    private readonly ICaseRelationService _caseRelationService;

    public DetailsModel(
        ICaseService caseService,
        ICaseInvestigationService investigationService,
        ICaseResolutionService caseResolutionService,
        IDepartmentRepository departmentRepository,
        ICatalogRepository catalogRepository,
        IAttachmentRepository attachmentRepository,
        IFileStorage fileStorage,
        IAuthorizationService authorizationService,
        IKnowledgeService knowledgeService,
        ICaseRelationService caseRelationService)
    {
        _caseService = caseService;
        _investigationService = investigationService;
        _caseResolutionService = caseResolutionService;
        _departmentRepository = departmentRepository;
        _catalogRepository = catalogRepository;
        _attachmentRepository = attachmentRepository;
        _fileStorage = fileStorage;
        _authorizationService = authorizationService;
        _knowledgeService = knowledgeService;
        _caseRelationService = caseRelationService;
    }

    public CaseDto Case { get; set; } = null!;
    public CaseInvestigationTimelineDto InvestigationTimeline { get; set; } = null!;
    public CaseResolutionDto? Resolution { get; set; }
    public CaseRelationsOverviewDto RelationsOverview { get; set; } = null!;
    public bool CanDiagnose { get; set; }
    public bool CanResolve { get; set; }
    public bool CanReopen { get; set; }
    public bool CanCreateKnowledge { get; set; }
    public bool CanRelate { get; set; }
    public IReadOnlyList<ComponentDependency> RelatedDependencies { get; set; } = [];
    public IReadOnlyList<CaseEvidenceDto> Evidences { get; set; } = [];

    // Inputs para relacionamento manual (Fase 8)
    [BindProperty]
    public string ManualRelationTypeInput { get; set; } = "Duplicate";
    [BindProperty]
    public ulong ManualTargetCaseNumberInput { get; set; }

    [BindProperty]
    public string ReopenReasonInput { get; set; } = string.Empty;

    // Inputs para cadastro de evidência estruturada (Bloco 7.A.4)
    [BindProperty]
    public string EvidenceTypeInput { get; set; } = "Log";
    [BindProperty]
    public string EvidenceDescriptionInput { get; set; } = string.Empty;
    [BindProperty]
    public long? EvidenceHypothesisIdInput { get; set; }
    [BindProperty]
    public string EvidenceRelationTypeInput { get; set; } = "Supports";
    [BindProperty]
    public string? EvidenceJustificationInput { get; set; }
    [BindProperty]
    public long? EvidenceDiagnosticStepIdInput { get; set; }

    // Lookups para encerramento
    public IReadOnlyList<RootCauseDto> AvailableRootCauses { get; set; } = [];
    public IReadOnlyList<Department> AvailableDepartments { get; set; } = [];
    public long? SuggestedComponentId { get; set; }
    public long? SuggestedDepartmentId { get; set; }

    // Métricas da investigação (tentativas e tempo)
    public int FailedAttemptsCount { get; set; }
    public int SuccessfulAttemptsCount { get; set; }
    public int TotalDurationSeconds { get; set; }
    public double ElapsedMinutes { get; set; }

    [BindProperty]
    public string? NormalizedSummaryInput { get; set; }

    // Inputs para cadastro de hipótese (Fase 4)
    [BindProperty]
    public string HypothesisTitleInput { get; set; } = string.Empty;
    [BindProperty]
    public string? HypothesisDescriptionInput { get; set; }
    [BindProperty]
    public long? HypothesisComponentIdInput { get; set; }

    // Inputs para registro de teste/tentativa (BR-025, BR-026)
    [BindProperty]
    public long? StepHypothesisIdInput { get; set; }
    [BindProperty]
    public string StepTitleInput { get; set; } = string.Empty;
    [BindProperty]
    public string StepObjectiveInput { get; set; } = string.Empty;
    [BindProperty]
    public string? StepInstructionInput { get; set; }
    [BindProperty]
    public string StepInputEvidenceSummaryInput { get; set; } = string.Empty;
    [BindProperty]
    public string StepResultSummaryInput { get; set; } = string.Empty;
    [BindProperty]
    public string StepOutcomeInput { get; set; } = nameof(DiagnosticStepOutcome.Worked);
    [BindProperty]
    public string StepRiskLevelInput { get; set; } = "Low";
    [BindProperty]
    public int? StepDurationSecondsInput { get; set; }

    // Inputs para avaliação de hipótese (BR-024)
    [BindProperty]
    public long EvaluateHypothesisIdInput { get; set; }
    [BindProperty]
    public string EvaluateNewStatusInput { get; set; } = nameof(HypothesisStatus.Discarded);
    [BindProperty]
    public string EvaluateJustificationInput { get; set; } = string.Empty;

    // Inputs para Encerramento Estruturado (Fase 5 / Bloco 5.1 / BR-027 / BR-028)
    [BindProperty]
    public string ResolutionSummaryInput { get; set; } = string.Empty;
    [BindProperty]
    public string ValidationSummaryInput { get; set; } = string.Empty;
    [BindProperty]
    public long? RootCauseIdInput { get; set; }
    [BindProperty]
    public string? NewRootCauseNameInput { get; set; }
    [BindProperty]
    public string? NewRootCauseCategoryInput { get; set; }
    [BindProperty]
    public bool RootCauseConfirmedInput { get; set; }
    [BindProperty]
    public long? ResponsibleComponentIdInput { get; set; }
    [BindProperty]
    public long? ResponsibleDepartmentIdInput { get; set; }
    [BindProperty]
    public string ResolutionTypeInput { get; set; } = "Definitive";
    [BindProperty]
    public string RecurrenceRiskInput { get; set; } = "Low";
    [BindProperty]
    public string? RecurrenceNotesInput { get; set; }
    [BindProperty]
    public string? PreventiveActionsInput { get; set; }
    [BindProperty]
    public int? EffortMinutesInput { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var item = await _caseService.GetCaseByIdAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        Case = item;
        NormalizedSummaryInput = item.NormalizedSummary;

        InvestigationTimeline = await _investigationService.GetInvestigationTimelineAsync(id);
        Resolution = await _caseResolutionService.GetResolutionByCaseIdAsync(id);
        Evidences = await _investigationService.GetEvidencesByCaseIdAsync(id);

        var authDiagnose = await _authorizationService.AuthorizeAsync(User, "caso.diagnosticar");
        CanDiagnose = authDiagnose.Succeeded;

        var authResolve = await _authorizationService.AuthorizeAsync(User, "caso.encerrar");
        CanResolve = authResolve.Succeeded && Case.Status != "Resolved";
        CanReopen = authResolve.Succeeded && Case.Status == "Resolved";

        var authKnowledge = await _authorizationService.AuthorizeAsync(User, "solucao.criar");
        CanCreateKnowledge = authKnowledge.Succeeded;

        // Métricas de investigação para o cabeçalho e card de Lição Aprendida (DidNotWork vs Worked/PartiallyWorked)
        FailedAttemptsCount = InvestigationTimeline.Steps.Count(s => s.Outcome == "DidNotWork" || s.Outcome == "ConclusiveRefuted");
        SuccessfulAttemptsCount = InvestigationTimeline.Steps.Count(s => s.Outcome == "Worked" || s.Outcome == "PartiallyWorked" || s.Outcome == "ConclusiveSupported");
        TotalDurationSeconds = InvestigationTimeline.Steps.Sum(s => s.DurationSeconds ?? 0);

        var endRef = Resolution?.ResolvedAt ?? DateTime.UtcNow;
        ElapsedMinutes = Math.Round((endRef - Case.OpenedAt).TotalMinutes, 1);
        if (ElapsedMinutes < 0) ElapsedMinutes = 0;

        // Bloco 7.A.2: Topologia real de dependências dos componentes afetados
        var compIds = Case.AffectedComponents.Select(c => c.ComponentId).Distinct().ToList();
        var allDeps = await _catalogRepository.GetComponentDependenciesAsync();
        if (compIds.Count > 0)
        {
            RelatedDependencies = allDeps.Where(d => compIds.Contains(d.SourceComponentId) || compIds.Contains(d.TargetComponentId)).ToList();
        }
        else
        {
            RelatedDependencies = allDeps;
        }

        // Pré-sugestão de componente e departamento responsável
        if (CanResolve)
        {
            AvailableRootCauses = await _caseResolutionService.GetRootCausesAsync();
            AvailableDepartments = await _departmentRepository.GetAllAsync();

            // 1. Componente mais provável: da hipótese 'Supported' se houver, ou primeiro afetado
            var supportedHyp = InvestigationTimeline.Hypotheses.FirstOrDefault(h => h.Status == "Supported" && h.ComponentId.HasValue);
            SuggestedComponentId = supportedHyp?.ComponentId ?? Case.AffectedComponents.FirstOrDefault()?.ComponentId;

            // 2. Departamento sugerido a partir do dono primário (OwnerDepartmentId) do componente
            if (SuggestedComponentId.HasValue)
            {
                var compEntity = await _catalogRepository.GetComponentByIdAsync(SuggestedComponentId.Value);
                SuggestedDepartmentId = compEntity?.OwnerDepartmentId;
            }

            ResponsibleComponentIdInput ??= SuggestedComponentId;
            ResponsibleDepartmentIdInput ??= SuggestedDepartmentId;
        }

        // Fase 8: Casos Relacionados determinísticos e Insight Agregado
        RelationsOverview = await _caseRelationService.GetCaseRelationsOverviewAsync(id);
        var authRelate = await _authorizationService.AuthorizeAsync(User, "caso.relacionar");
        CanRelate = authRelate.Succeeded;

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateSummaryAsync(long id)
    {
        long? currentUserId = GetCurrentUserId();

        await _caseService.UpdateNormalizedSummaryAsync(id, NormalizedSummaryInput, currentUserId);
        StatusMessage = "Resumo normalizado atualizado com sucesso (BR-021). O relato original permaneceu intacto.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRegisterHypothesisAsync(long id)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, "caso.diagnosticar");
        if (!authResult.Succeeded) return Forbid();

        try
        {
            var command = new RegisterHypothesisCommand(
                CaseId: id,
                Title: HypothesisTitleInput,
                Description: HypothesisDescriptionInput,
                ComponentId: HypothesisComponentIdInput
            );

            await _investigationService.RegisterHypothesisAsync(command, GetCurrentUserId());
            StatusMessage = "Hipótese registrada com sucesso com status 'Proposed' (BR-023).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar hipótese: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRegisterStepAsync(long id)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, "caso.diagnosticar");
        if (!authResult.Succeeded) return Forbid();

        try
        {
            var command = new RegisterDiagnosticStepCommand(
                CaseId: id,
                HypothesisId: StepHypothesisIdInput > 0 ? StepHypothesisIdInput : null,
                Title: StepTitleInput,
                Objective: StepObjectiveInput,
                Instruction: StepInstructionInput,
                InputEvidenceSummary: StepInputEvidenceSummaryInput,
                ResultSummary: StepResultSummaryInput,
                Outcome: StepOutcomeInput,
                StepType: DiagnosticStepTypes.Verification,
                RiskLevel: StepRiskLevelInput,
                DurationSeconds: StepDurationSecondsInput
            );

            await _investigationService.RegisterDiagnosticStepAsync(command, GetCurrentUserId());
            StatusMessage = $"Ação diagnóstica registrada com sucesso. Resultado classificado como '{StepOutcomeInput}' (BR-025, BR-026).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar ação diagnóstica: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostEvaluateHypothesisAsync(long id)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, "caso.diagnosticar");
        if (!authResult.Succeeded) return Forbid();

        try
        {
            var command = new EvaluateHypothesisCommand(
                HypothesisId: EvaluateHypothesisIdInput,
                NewStatus: EvaluateNewStatusInput,
                Justification: EvaluateJustificationInput
            );

            await _investigationService.EvaluateHypothesisAsync(command, GetCurrentUserId());
            StatusMessage = $"Hipótese avaliada e atualizada para '{EvaluateNewStatusInput}' com justificativa registrada (BR-024).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao avaliar hipótese: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResolveCaseAsync(long id)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, "caso.encerrar");
        if (!authResult.Succeeded) return Forbid();

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Forbid();

        try
        {
            var command = new ResolveCaseCommand(
                CaseId: id,
                ResolutionSummary: ResolutionSummaryInput,
                ValidationSummary: ValidationSummaryInput,
                RootCauseId: RootCauseIdInput > 0 ? RootCauseIdInput : null,
                NewRootCauseName: NewRootCauseNameInput,
                NewRootCauseCategory: NewRootCauseCategoryInput,
                RootCauseConfirmed: RootCauseConfirmedInput,
                ResponsibleComponentId: ResponsibleComponentIdInput > 0 ? ResponsibleComponentIdInput : null,
                ResponsibleDepartmentId: ResponsibleDepartmentIdInput > 0 ? ResponsibleDepartmentIdInput : null,
                ResolutionType: ResolutionTypeInput,
                RecurrenceRisk: RecurrenceRiskInput,
                RecurrenceNotes: RecurrenceNotesInput,
                PreventiveActions: PreventiveActionsInput,
                EffortMinutes: EffortMinutesInput
            );

            await _caseResolutionService.ResolveCaseAsync(command, currentUserId.Value);
            StatusMessage = "Caso encerrado com sucesso! A resolução estruturada e a lição aprendida foram registradas (BR-027, BR-028).";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao encerrar caso: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnGetDownloadAttachmentAsync(long attachmentId)
    {
        var att = await _attachmentRepository.GetByIdAsync(attachmentId);
        if (att == null)
        {
            return NotFound("Anexo não encontrado.");
        }

        var stream = await _fileStorage.GetAsync(att.StorageKey);
        if (stream == null)
        {
            return NotFound("Arquivo físico não encontrado no storage.");
        }

        return File(stream, att.MimeType, att.FileName);
    }

    public async Task<IActionResult> OnPostCreateKnowledgeDraftAsync(long id)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "solucao.criar");
        if (!auth.Succeeded) return Forbid();

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Challenge();

        try
        {
            var knowledgeId = await _knowledgeService.CreateDraftFromCaseAsync(new CreateDraftFromCaseCommand(id), currentUserId.Value);
            StatusMessage = "Rascunho de artigo gerado na Base de Conhecimento a partir da Lição Aprendida deste caso com sucesso (BR-046)!";
            return RedirectToPage("/Knowledge/Details", new { id = knowledgeId });
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao gerar rascunho de conhecimento: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReopenAsync(long id)
    {
        var authResolve = await _authorizationService.AuthorizeAsync(User, "caso.encerrar");
        if (!authResolve.Succeeded) return Forbid();

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Challenge();

        if (string.IsNullOrWhiteSpace(ReopenReasonInput))
        {
            ErrorMessage = "O motivo da reabertura do caso é obrigatório.";
            return RedirectToPage(new { id });
        }

        try
        {
            await _caseService.ReopenCaseAsync(id, ReopenReasonInput.Trim(), currentUserId.Value);
            StatusMessage = "Caso reaberto com sucesso! Uma nova iteração de investigação foi iniciada.";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao reabrir caso: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRecordEvidenceAsync(long id)
    {
        var authDiagnose = await _authorizationService.AuthorizeAsync(User, "caso.diagnosticar");
        if (!authDiagnose.Succeeded) return Forbid();

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Challenge();

        if (string.IsNullOrWhiteSpace(EvidenceDescriptionInput))
        {
            ErrorMessage = "A descrição da evidência é obrigatória.";
            return RedirectToPage(new { id });
        }

        try
        {
            var relations = new List<HypothesisEvidenceRelationInputDto>();
            if (EvidenceHypothesisIdInput.HasValue && EvidenceHypothesisIdInput.Value > 0)
            {
                relations.Add(new HypothesisEvidenceRelationInputDto(
                    EvidenceHypothesisIdInput.Value,
                    EvidenceRelationTypeInput,
                    EvidenceJustificationInput
                ));
            }

            await _investigationService.RecordEvidenceAsync(new RecordEvidenceCommand(
                CaseId: id,
                EvidenceType: EvidenceTypeInput,
                Description: EvidenceDescriptionInput.Trim(),
                DiagnosticStepId: EvidenceDiagnosticStepIdInput,
                HypothesisRelations: relations
            ), currentUserId.Value);

            StatusMessage = "Evidência factual registrada com sucesso e vinculada à investigação!";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar evidência: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddRelationAsync(long id)
    {
        var authRelate = await _authorizationService.AuthorizeAsync(User, "caso.relacionar");
        if (!authRelate.Succeeded) return Forbid();

        if (ManualTargetCaseNumberInput == 0)
        {
            ErrorMessage = "Informe um número de caso válido para relacionar.";
            return RedirectToPage(new { id, activeTab = "related" });
        }

        try
        {
            var currentUserId = GetCurrentUserId() ?? 1L;
            await _caseRelationService.CreateManualRelationAsync(new CreateCaseRelationCommand(
                SourceCaseId: id,
                TargetCaseNumber: ManualTargetCaseNumberInput,
                RelationType: ManualRelationTypeInput
            ), currentUserId);

            StatusMessage = "Relacionamento entre casos registrado com sucesso.";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao relacionar caso: {ex.Message}";
        }

        return RedirectToPage(new { id, activeTab = "related" });
    }

    private long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(userIdClaim, out var parsedId) ? parsedId : null;
    }
}
