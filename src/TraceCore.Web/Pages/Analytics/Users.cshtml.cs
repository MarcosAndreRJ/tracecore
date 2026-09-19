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
public class UsersModel : PageModel
{
    private readonly IManagementAnalyticsService _analyticsService;
    private readonly IDepartmentRepository _departmentRepository;

    public UsersModel(
        IManagementAnalyticsService analyticsService,
        IDepartmentRepository departmentRepository)
    {
        _analyticsService = analyticsService;
        _departmentRepository = departmentRepository;
    }

    [BindProperty(SupportsGet = true)]
    public AnalyticsFilterDto Filter { get; set; } = new();

    public UserAnalyticsDto Analytics { get; private set; } = new();
    public IReadOnlyList<Department> AvailableDepartments { get; private set; } = [];

    public async Task OnGetAsync()
    {
        AvailableDepartments = await _departmentRepository.GetAllAsync();
        Analytics = await _analyticsService.GetUserAnalyticsAsync(Filter);
    }
}
