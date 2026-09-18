using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Application.Exceptions;

namespace TraceCore.Web.Pages.Diagnosis.Flows;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IDiagnosticEngineService _engineService;
    private readonly ICatalogService _catalogService;
    private readonly IAuthorizationService _authorizationService;

    public IndexModel(
        IDiagnosticEngineService engineService,
        ICatalogService catalogService,
        IAuthorizationService authorizationService)
    {
        _engineService = engineService;
        _catalogService = catalogService;
        _authorizationService = authorizationService;
    }

    public IReadOnlyList<DiagnosticFlowDto> Flows { get; private set; } = [];
    public DiagnosticFlowDetailsDto? SelectedFlowDetails { get; private set; }
    public IReadOnlyList<ComponentEntity> AvailableComponents { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public long? SelectedFlowId { get; set; }

    [BindProperty]
    public string NewFlowCodeInput { get; set; } = string.Empty;
    [BindProperty]
    public string NewFlowNameInput { get; set; } = string.Empty;
    [BindProperty]
    public string NewFlowKeywordsInput { get; set; } = string.Empty;
    [BindProperty]
    public string? NewFlowDescriptionInput { get; set; }

    [BindProperty]
    public string NewHypothesisTitleInput { get; set; } = string.Empty;
    [BindProperty]
    public string? NewHypothesisDescriptionInput { get; set; }
    [BindProperty]
    public long? NewHypothesisComponentIdInput { get; set; }

    [BindProperty]
    public string NewCheckCodeInput { get; set; } = string.Empty;
    [BindProperty]
    public string NewCheckTitleInput { get; set; } = string.Empty;
    [BindProperty]
    public string NewCheckQuestionInput { get; set; } = string.Empty;
    [BindProperty]
    public int NewCheckCostInput { get; set; } = 1;
    [BindProperty]
    public string NewCheckRiskInput { get; set; } = "Low";

    [BindProperty]
    public string NewOptionTextInput { get; set; } = string.Empty;
    [BindProperty]
    public long NewOptionTargetHypothesisIdInput { get; set; }
    [BindProperty]
    public string NewOptionImpactTypeInput { get; set; } = "Favors";
    [BindProperty]
    public decimal NewOptionImpactWeightInput { get; set; } = 1.5m;

    [TempData]
    public string? StatusMessage { get; set; }
    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var authConfig = await _authorizationService.AuthorizeAsync(User, "diagnostico.configurar");
        if (!authConfig.Succeeded) return Forbid();

        Flows = await _engineService.GetAllFlowsAsync(activeOnly: false);
        AvailableComponents = await _catalogService.GetAllComponentsAsync();

        if (SelectedFlowId.HasValue)
        {
            SelectedFlowDetails = await _engineService.GetFlowDetailsAsync(SelectedFlowId.Value);
        }
        else if (Flows.Count > 0)
        {
            SelectedFlowId = Flows[0].Id;
            SelectedFlowDetails = await _engineService.GetFlowDetailsAsync(SelectedFlowId.Value);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateFlowAsync()
    {
        var authConfig = await _authorizationService.AuthorizeAsync(User, "diagnostico.configurar");
        if (!authConfig.Succeeded) return Forbid();

        try
        {
            var userId = GetCurrentUserId() ?? 1L;
            long flowId = await _engineService.CreateFlowAsync(new CreateDiagnosticFlowCommand(
                Code: NewFlowCodeInput,
                Name: NewFlowNameInput,
                EntryKeywords: NewFlowKeywordsInput,
                Description: NewFlowDescriptionInput
            ), userId);

            StatusMessage = "Fluxo de diagnóstico criado com sucesso!";
            return RedirectToPage(new { SelectedFlowId = flowId });
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao criar fluxo: {ex.Message}";
        }

        return RedirectToPage(new { SelectedFlowId });
    }

    public async Task<IActionResult> OnPostAddHypothesisAsync(long flowId)
    {
        var authConfig = await _authorizationService.AuthorizeAsync(User, "diagnostico.configurar");
        if (!authConfig.Succeeded) return Forbid();

        try
        {
            await _engineService.AddFlowHypothesisAsync(
                flowId,
                NewHypothesisTitleInput,
                NewHypothesisDescriptionInput,
                NewHypothesisComponentIdInput
            );

            StatusMessage = "Hipótese candidata vinculada ao fluxo com sucesso!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao adicionar hipótese: {ex.Message}";
        }

        return RedirectToPage(new { SelectedFlowId = flowId });
    }

    public async Task<IActionResult> OnPostAddCheckAsync(long flowId)
    {
        var authConfig = await _authorizationService.AuthorizeAsync(User, "diagnostico.configurar");
        if (!authConfig.Succeeded) return Forbid();

        try
        {
            await _engineService.AddCheckAsync(new CreateDiagnosticCheckCommand(
                FlowId: flowId,
                Code: NewCheckCodeInput,
                Title: NewCheckTitleInput,
                QuestionText: NewCheckQuestionInput,
                Cost: NewCheckCostInput,
                RiskLevel: NewCheckRiskInput
            ));

            StatusMessage = "Verificação cadastrada com sucesso no fluxo!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao cadastrar verificação: {ex.Message}";
        }

        return RedirectToPage(new { SelectedFlowId = flowId });
    }

    public async Task<IActionResult> OnPostAddOptionAsync(long flowId, long checkId)
    {
        var authConfig = await _authorizationService.AuthorizeAsync(User, "diagnostico.configurar");
        if (!authConfig.Succeeded) return Forbid();

        try
        {
            var impacts = new List<(long HypothesisId, string ImpactType, decimal Weight)>();
            if (NewOptionTargetHypothesisIdInput > 0)
            {
                impacts.Add((NewOptionTargetHypothesisIdInput, NewOptionImpactTypeInput, NewOptionImpactWeightInput));
            }

            await _engineService.AddCheckOptionWithImpactsAsync(
                checkId,
                NewOptionTextInput,
                orderNo: 1,
                impacts: impacts
            );

            StatusMessage = "Opção de resposta com impacto cadastrada com sucesso!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao adicionar opção: {ex.Message}";
        }

        return RedirectToPage(new { SelectedFlowId = flowId });
    }

    private long? GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(idStr, out var id) ? id : null;
    }
}
