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
using TraceCore.Domain.Repositories;

namespace TraceCore.Web.Pages.Knowledge;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly ICaseRepository _caseRepository;
    private readonly IAuthorizationService _authorizationService;

    public DetailsModel(
        IKnowledgeService knowledgeService,
        ICaseRepository caseRepository,
        IAuthorizationService authorizationService)
    {
        _knowledgeService = knowledgeService;
        _caseRepository = caseRepository;
        _authorizationService = authorizationService;
    }

    public KnowledgeDetailViewModel Item { get; private set; } = default!;

    // Permissões
    public bool CanCreate { get; private set; }
    public bool CanReview { get; private set; }
    public bool CanPublish { get; private set; }

    // Inputs para nova versão (BR-042)
    [BindProperty]
    public string? NewVersionContentMarkdown { get; set; }
    [BindProperty]
    public string? NewVersionValidationMethod { get; set; }
    [BindProperty]
    public string? NewVersionRiskWarning { get; set; }
    [BindProperty]
    public string? NewVersionRollbackPlan { get; set; }
    [BindProperty]
    public string? NewVersionChangeSummary { get; set; }

    // Input para arquivar/cancelar a solução (libera nova solução para o mesmo caso)
    [BindProperty]
    public string? ArchiveReasonInput { get; set; }

    // Inputs para registrar uso (BR-047, BR-048)
    [BindProperty]
    public long UsageCaseId { get; set; }
    [BindProperty]
    public string UsageOutcome { get; set; } = "Worked";
    [BindProperty]
    public string? UsageNotes { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var detail = await _knowledgeService.GetKnowledgeDetailAsync(id);
        if (detail == null)
        {
            ErrorMessage = "Artigo de conhecimento não encontrado.";
            return RedirectToPage("/Knowledge/Index");
        }

        var authCreate = await _authorizationService.AuthorizeAsync(User, "solucao.criar");
        var authReview = await _authorizationService.AuthorizeAsync(User, "solucao.validar");
        var authPublish = await _authorizationService.AuthorizeAsync(User, "solucao.publicar");

        CanCreate = authCreate.Succeeded;
        CanReview = authReview.Succeeded;
        CanPublish = authPublish.Succeeded;

        Item = new KnowledgeDetailViewModel
        {
            Id = detail.Id,
            Code = detail.Code,
            Title = detail.Title,
            Version = detail.Version,
            LifecycleStatus = detail.LifecycleStatus,
            IsAiGenerated = detail.IsAiGenerated,
            Author = detail.Author,
            Reviewer = detail.Reviewer,
            PublishedAt = detail.PublishedAt ?? DateTime.MinValue,
            LastReviewedAt = detail.LastReviewedAt ?? DateTime.MinValue,
            NextReviewDue = detail.NextReviewDue ?? DateTime.MinValue,
            ProvenanceType = detail.ProvenanceType,
            SourceCaseId = detail.SourceCaseId,
            SourceCaseTitle = detail.SourceCaseTitle,
            SourceReference = detail.SourceReference,
            ProductCode = detail.ProductCode,
            ProductName = detail.ProductName,
            Component = detail.Component,
            ApplicableVersions = detail.ApplicableVersions,
            ApplicableEnvironments = detail.ApplicableEnvironments,
            Prerequisites = detail.Prerequisites,
            ProblemDescription = detail.ProblemDescription,
            RiskLevel = detail.RiskLevel,
            RiskWarning = detail.RiskWarning,
            RollbackPlan = detail.RollbackPlan,
            Steps = detail.Steps.Select(s => new ProcedureStepViewModel
            {
                StepNumber = s.StepNumber,
                Title = s.Title,
                Description = s.Description,
                Command = s.Command,
                ExpectedOutput = s.ExpectedOutput
            }).ToList(),
            ValidationMethod = detail.ValidationMethod,
            ValidationCommand = detail.ValidationCommand,
            ExpectedValidationOutput = detail.ExpectedValidationOutput,
            TotalUsages = detail.TotalUsages,
            SuccessfulUsages = detail.SuccessfulUsages,
            SuccessRate = detail.SuccessRate,
            AssociatedCases = detail.AssociatedCases.Select(c => new AssociatedCaseRefViewModel
            {
                CaseId = c.CaseId,
                Code = c.Code,
                Title = c.Title,
                ResolutionDate = c.ResolutionDate,
                Outcome = c.Outcome
            }).ToList(),
            Revisions = detail.Revisions.Select(r => new RevisionHistoryViewModel
            {
                Version = r.Version,
                ChangedBy = r.ChangedBy,
                ChangedAt = r.ChangedAt,
                Reason = r.Reason
            }).ToList()
        };

        NewVersionValidationMethod ??= detail.ValidationMethod;
        NewVersionRiskWarning ??= detail.RiskWarning;
        NewVersionRollbackPlan ??= detail.RollbackPlan;

        return Page();
    }

    public async Task<IActionResult> OnPostSubmitForReviewAsync(long id)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "solucao.validar");
        if (!auth.Succeeded) return Forbid();

        long? userId = GetCurrentUserId();
        if (!userId.HasValue) return Challenge();

        try
        {
            await _knowledgeService.SubmitForReviewAsync(new SubmitForReviewCommand(id), userId.Value);
            StatusMessage = "Item de conhecimento submetido com sucesso para o ciclo de revisão formal (BR-040).";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao submeter para revisão: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAndPublishAsync(long id)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "solucao.publicar");
        if (!auth.Succeeded) return Forbid();

        long? userId = GetCurrentUserId();
        if (!userId.HasValue) return Challenge();

        try
        {
            await _knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(id), userId.Value);
            StatusMessage = "Solução aprovada e publicada oficialmente na base corporativa com sucesso (BR-041).";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao aprovar e publicar: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCreateNewVersionAsync(long id)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "solucao.criar");
        if (!auth.Succeeded) return Forbid();

        long? userId = GetCurrentUserId();
        if (!userId.HasValue) return Challenge();

        try
        {
            var newVer = await _knowledgeService.CreateNewVersionAsync(new CreateNewVersionCommand(
                KnowledgeItemId: id,
                ContentMarkdown: NewVersionContentMarkdown ?? string.Empty,
                ValidationMethod: NewVersionValidationMethod,
                RiskWarning: NewVersionRiskWarning,
                RollbackPlan: NewVersionRollbackPlan,
                ChangeSummary: NewVersionChangeSummary ?? "Revisão e atualização de diretrizes operacionais"
            ), userId.Value);

            StatusMessage = $"Nova versão v{newVer}.0 criada com sucesso (BR-042). A versão anterior permanece intacta para fins de auditoria.";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao criar nova versão: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeprecateAsync(long id)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "solucao.publicar");
        if (!auth.Succeeded) return Forbid();

        long? userId = GetCurrentUserId();
        if (!userId.HasValue) return Challenge();

        try
        {
            await _knowledgeService.DeprecateKnowledgeAsync(new DeprecateKnowledgeCommand(id), userId.Value);
            StatusMessage = "Item de conhecimento descontinuado com sucesso (BR-049). Ele não será mais sugerido por padrão, mas seu histórico foi mantido.";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao descontinuar item: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostArchiveAsync(long id)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "solucao.criar");
        if (!auth.Succeeded) return Forbid();

        long? userId = GetCurrentUserId();
        if (!userId.HasValue) return Challenge();

        try
        {
            await _knowledgeService.ArchiveKnowledgeAsync(new ArchiveKnowledgeCommand(id, ArchiveReasonInput), userId.Value);
            StatusMessage = "Solução arquivada/cancelada com sucesso. Já é possível gerar uma nova solução para o caso de origem, se necessário.";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao arquivar solução: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRecordUsageAsync(long id)
    {
        long? userId = GetCurrentUserId();
        if (!userId.HasValue) return Challenge();

        try
        {
            await _knowledgeService.RecordUsageAsync(new RecordKnowledgeUsageCommand(
                KnowledgeItemId: id,
                CaseId: UsageCaseId,
                Outcome: UsageOutcome,
                Notes: UsageNotes
            ), userId.Value);

            StatusMessage = "Utilização da solução registrada com sucesso em incidente real! A taxa de eficácia foi recalculada (BR-047, BR-048).";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar utilização: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    private long? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(idClaim, out var id)) return id;
        return null;
    }
}

public class KnowledgeDetailViewModel
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = "v1.0";
    public string LifecycleStatus { get; set; } = "Published";
    public bool IsAiGenerated { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Reviewer { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public DateTime LastReviewedAt { get; set; }
    public DateTime NextReviewDue { get; set; }

    public string ProvenanceType { get; set; } = "case";
    public long? SourceCaseId { get; set; }
    public string SourceCaseTitle { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string ApplicableVersions { get; set; } = string.Empty;
    public List<string> ApplicableEnvironments { get; set; } = [];
    public List<string> Prerequisites { get; set; } = [];

    public string ProblemDescription { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public string RiskWarning { get; set; } = string.Empty;
    public string RollbackPlan { get; set; } = string.Empty;

    public List<ProcedureStepViewModel> Steps { get; set; } = [];

    public string ValidationMethod { get; set; } = string.Empty;
    public string? ValidationCommand { get; set; }
    public string? ExpectedValidationOutput { get; set; }

    public int TotalUsages { get; set; }
    public int SuccessfulUsages { get; set; }
    public string SuccessRate { get; set; } = string.Empty;
    public List<AssociatedCaseRefViewModel> AssociatedCases { get; set; } = [];
    public List<RevisionHistoryViewModel> Revisions { get; set; } = [];
}

public class ProcedureStepViewModel
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Command { get; set; }
    public string? ExpectedOutput { get; set; }
}

public class AssociatedCaseRefViewModel
{
    public long CaseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime ResolutionDate { get; set; }
    public string Outcome { get; set; } = string.Empty;
}

public class RevisionHistoryViewModel
{
    public string Version { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}
