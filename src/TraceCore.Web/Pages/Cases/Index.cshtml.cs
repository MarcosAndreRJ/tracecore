using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Web.Pages.Cases;

[Authorize(Policy = "caso.visualizar")]
public class IndexModel : PageModel
{
    private readonly ICaseService _caseService;
    private readonly IClientRepository _clientRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public IndexModel(
        ICaseService caseService,
        IClientRepository clientRepository,
        ICatalogRepository catalogRepository,
        IDepartmentRepository departmentRepository)
    {
        _caseService = caseService;
        _clientRepository = clientRepository;
        _catalogRepository = catalogRepository;
        _departmentRepository = departmentRepository;
    }

    // Parâmetros de Filtro
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Severity { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? ClientId { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? ProductId { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? DepartmentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Period { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalItems / PageSize));

    // KPIs Superiores
    public int MetricTotalOpen { get; set; }
    public int MetricCritical { get; set; }
    public int MetricHigh { get; set; }
    public int MetricClosed { get; set; }

    // Listagens
    public IReadOnlyList<CaseDto> Cases { get; set; } = new List<CaseDto>();
    public IReadOnlyList<Client> Clients { get; set; } = new List<Client>();
    public IReadOnlyList<Product> Products { get; set; } = new List<Product>();
    public IReadOnlyList<Department> Departments { get; set; } = new List<Department>();

    public async Task OnGetAsync()
    {
        var allCases = await _caseService.GetAllCasesAsync();

        // Carregar Lookups para combos de filtro
        Clients = await _clientRepository.GetAllAsync();
        Products = await _catalogRepository.GetAllProductsAsync();
        Departments = await _departmentRepository.GetAllAsync();

        // KPIs Operacionais
        MetricTotalOpen = allCases.Count(c => c.Status.Equals("Open", StringComparison.OrdinalIgnoreCase));
        MetricCritical = allCases.Count(c => c.Status.Equals("Open", StringComparison.OrdinalIgnoreCase) && c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase));
        MetricHigh = allCases.Count(c => c.Status.Equals("Open", StringComparison.OrdinalIgnoreCase) && c.Severity.Equals("High", StringComparison.OrdinalIgnoreCase));
        MetricClosed = allCases.Count(c => c.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase));

        // Aplicar Filtros
        var query = allCases.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            query = query.Where(c =>
                c.CaseNumber.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(c.NormalizedSummary) && c.NormalizedSummary.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.ObservedBehavior) && c.ObservedBehavior.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.OriginalReport) && c.OriginalReport.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.ErrorCode) && c.ErrorCode.Contains(term, StringComparison.OrdinalIgnoreCase))
            );
        }

        if (!string.IsNullOrWhiteSpace(Status))
        {
            query = query.Where(c => c.Status.Equals(Status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(Severity))
        {
            query = query.Where(c => c.Severity.Equals(Severity, StringComparison.OrdinalIgnoreCase));
        }

        if (ClientId.HasValue && ClientId.Value > 0)
        {
            query = query.Where(c => c.ClientId == ClientId.Value);
        }

        if (ProductId.HasValue && ProductId.Value > 0)
        {
            query = query.Where(c => c.ProductId == ProductId.Value);
        }

        if (DepartmentId.HasValue && DepartmentId.Value > 0)
        {
            query = query.Where(c => c.CurrentDepartmentId == DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(Period) && Period != "all")
        {
            var now = DateTime.UtcNow;
            query = Period switch
            {
                "today" => query.Where(c => c.OpenedAt >= now.Date),
                "week" => query.Where(c => c.OpenedAt >= now.AddDays(-7)),
                "month" => query.Where(c => c.OpenedAt >= now.AddDays(-30)),
                _ => query
            };
        }

        // Ordenação: primeiro por severidade crítica em aberto, depois data descendente
        var filteredList = query
            .OrderByDescending(c => c.OpenedAt)
            .ToList();

        TotalItems = filteredList.Count;

        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages && TotalItems > 0) PageNumber = TotalPages;

        Cases = filteredList
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }
}
