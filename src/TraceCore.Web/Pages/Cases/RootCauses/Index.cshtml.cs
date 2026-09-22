using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Application.Services;

namespace TraceCore.Web.Pages.Cases.RootCauses;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICaseResolutionService _caseResolutionService;
    private readonly IAuthorizationService _authorizationService;

    public IndexModel(ICaseResolutionService caseResolutionService, IAuthorizationService authorizationService)
    {
        _caseResolutionService = caseResolutionService;
        _authorizationService = authorizationService;
    }

    public IReadOnlyList<RootCauseDto> RootCauses { get; private set; } = [];
    public Dictionary<long, int> UsageCounts { get; private set; } = [];
    public bool CanManage { get; private set; }

    [BindProperty]
    public long EditIdInput { get; set; }
    [BindProperty]
    public string NameInput { get; set; } = string.Empty;
    [BindProperty]
    public string? CodeInput { get; set; }
    [BindProperty]
    public string? CategoryInput { get; set; }
    [BindProperty]
    public string? DescriptionInput { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }
    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "caso.encerrar");
        if (!auth.Succeeded) return Forbid();

        CanManage = true;
        RootCauses = await _caseResolutionService.GetRootCausesAsync();

        UsageCounts = new Dictionary<long, int>();
        foreach (var rc in RootCauses)
        {
            UsageCounts[rc.Id] = await _caseResolutionService.CountResolutionsUsingRootCauseAsync(rc.Id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "caso.encerrar");
        if (!auth.Succeeded) return Forbid();

        try
        {
            await _caseResolutionService.CreateRootCauseAsync(NameInput, CodeInput, CategoryInput, DescriptionInput);
            StatusMessage = "Causa raiz cadastrada com sucesso no catálogo corporativo!";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio ({ex.RuleId}): {ex.Message}";
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Erro ao cadastrar causa raiz: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "caso.encerrar");
        if (!auth.Succeeded) return Forbid();

        try
        {
            await _caseResolutionService.UpdateRootCauseAsync(EditIdInput, NameInput, CodeInput, CategoryInput, DescriptionInput);
            StatusMessage = "Causa raiz atualizada com sucesso!";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio ({ex.RuleId}): {ex.Message}";
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar causa raiz: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id)
    {
        var auth = await _authorizationService.AuthorizeAsync(User, "caso.encerrar");
        if (!auth.Succeeded) return Forbid();

        try
        {
            var affectedResolutions = await _caseResolutionService.DeleteRootCauseAsync(id);
            StatusMessage = affectedResolutions > 0
                ? $"Causa raiz excluída. {affectedResolutions} resolução(ões) que a usavam ficaram sem causa raiz corporativa vinculada (o restante do registro foi mantido)."
                : "Causa raiz excluída do catálogo com sucesso.";
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Erro ao excluir causa raiz: {ex.Message}";
        }

        return RedirectToPage();
    }
}
