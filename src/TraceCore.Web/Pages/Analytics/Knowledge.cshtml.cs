using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;

namespace TraceCore.Web.Pages.Analytics;

[Authorize(Policy = "analytics.visualizar")]
public class KnowledgeModel : PageModel
{
    private readonly IManagementAnalyticsService _analyticsService;

    public KnowledgeModel(IManagementAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [BindProperty(SupportsGet = true)]
    public AnalyticsFilterDto Filter { get; set; } = new();

    public KnowledgeAnalyticsDto Analytics { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Analytics = await _analyticsService.GetKnowledgeAnalyticsAsync(Filter);
    }
}
