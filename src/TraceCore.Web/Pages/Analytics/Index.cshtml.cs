using System;
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
public class IndexModel : PageModel
{
    private readonly IManagementAnalyticsService _analyticsService;
    private readonly IClientRepository _clientRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public IndexModel(
        IManagementAnalyticsService analyticsService,
        IClientRepository clientRepository,
        ICatalogRepository catalogRepository,
        IDepartmentRepository departmentRepository)
    {
        _analyticsService = analyticsService;
        _clientRepository = clientRepository;
        _catalogRepository = catalogRepository;
        _departmentRepository = departmentRepository;
    }

    [BindProperty(SupportsGet = true)]
    public AnalyticsFilterDto Filter { get; set; } = new();

    public ManagementOverviewDto Overview { get; private set; } = new();

    // Lookups para os filtros
    public IReadOnlyList<Client> AvailableClients { get; private set; } = [];
    public IReadOnlyList<Product> AvailableProducts { get; private set; } = [];
    public IReadOnlyList<Department> AvailableDepartments { get; private set; } = [];

    public async Task OnGetAsync()
    {
        AvailableClients = await _clientRepository.GetAllAsync();
        AvailableProducts = await _catalogRepository.GetAllProductsAsync();
        AvailableDepartments = await _departmentRepository.GetAllAsync();

        Overview = await _analyticsService.GetOverviewAnalyticsAsync(Filter);
    }
}
