using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;

namespace TraceCore.Web.Pages.ContentQuality;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IContentPreparationService _contentPreparationService;

    public IndexModel(IContentPreparationService contentPreparationService)
    {
        _contentPreparationService = contentPreparationService;
    }

    [BindProperty(SupportsGet = true)]
    public string? SourceType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ValidationStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? QualityStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Visibility { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public ContentQualityMetricsDto Metrics { get; private set; } = null!;
    public ContentSearchResultDto SearchResult { get; private set; } = null!;

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!User.HasClaim("permission", "analytics.visualizar") && !User.HasClaim("permission", "conhecimento.visualizar"))
        {
            return Forbid();
        }

        Metrics = await _contentPreparationService.GetMetricsAsync(ct);

        var filter = new SearchableContentFilterDto(
            SourceType: SourceType,
            ValidationStatus: ValidationStatus,
            QualityStatus: QualityStatus,
            Visibility: Visibility,
            SearchTerm: SearchTerm,
            Page: PageNumber < 1 ? 1 : PageNumber,
            PageSize: 25
        );

        SearchResult = await _contentPreparationService.SearchAsync(filter, ct);

        return Page();
    }

    public async Task<IActionResult> OnPostSyncAllAsync(CancellationToken ct)
    {
        if (!User.HasClaim("permission", "analytics.visualizar") && !User.HasClaim("permission", "conhecimento.visualizar"))
        {
            return Forbid();
        }

        long? currentUserId = null;
        var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(subClaim, out var uid))
        {
            currentUserId = uid;
        }

        var knowledgeCount = await _contentPreparationService.SyncAllPublishedKnowledgeAsync(currentUserId, ct);
        var casesCount = await _contentPreparationService.SyncAllResolvedCasesAsync(currentUserId, ct);

        SuccessMessage = $"Sincronização concluída com sucesso: {knowledgeCount} artigos publicados e {casesCount} casos resolvidos estruturados e preparados.";

        return RedirectToPage();
    }
}
