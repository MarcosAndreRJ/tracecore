using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Web.Pages.Analytics;

[Authorize(Policy = "analytics.visualizar")]
public class DepartmentsModel : PageModel
{
    private readonly IManagementAnalyticsService _analyticsService;
    private readonly ICatalogRepository _catalogRepository;

    public DepartmentsModel(
        IManagementAnalyticsService analyticsService,
        ICatalogRepository catalogRepository)
    {
        _analyticsService = analyticsService;
        _catalogRepository = catalogRepository;
    }

    [BindProperty(SupportsGet = true)]
    public AnalyticsFilterDto Filter { get; set; } = new();

    public DepartmentAnalyticsDto Analytics { get; private set; } = new();
    public IReadOnlyList<Product> AvailableProducts { get; private set; } = [];

    public async Task OnGetAsync()
    {
        AvailableProducts = await _catalogRepository.GetAllProductsAsync();
        Analytics = await _analyticsService.GetDepartmentAnalyticsAsync(Filter);
    }
}
