using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Web.Pages;

[Authorize(Policy = "caso.visualizar")]
public class SearchModel : PageModel
{
    private readonly ISearchService _searchService;
    private readonly ICatalogRepository _catalogRepository;
    private readonly ICaseResolutionService _caseResolutionService;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IClientRepository _clientRepository;

    public SearchModel(
        ISearchService searchService,
        ICatalogRepository catalogRepository,
        ICaseResolutionService caseResolutionService,
        IDepartmentRepository departmentRepository,
        IClientRepository clientRepository)
    {
        _searchService = searchService;
        _catalogRepository = catalogRepository;
        _caseResolutionService = caseResolutionService;
        _departmentRepository = departmentRepository;
        _clientRepository = clientRepository;
    }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? ProductId { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? ProductVersionId { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? ComponentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? ClientId { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? ClientUnitId { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? DepartmentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? TechnologyId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ErrorCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? RootCauseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Environment { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? ContextCaseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedType { get; set; } = "All";

    // Dados de Resultado
    public SearchResultsResponseDto Results { get; private set; } = default!;

    // Lookups para os filtros do painel
    public IReadOnlyList<Product> AvailableProducts { get; private set; } = [];
    public IReadOnlyList<ComponentEntity> AvailableComponents { get; private set; } = [];
    public IReadOnlyList<RootCauseDto> AvailableRootCauses { get; private set; } = [];
    public IReadOnlyList<Department> AvailableDepartments { get; private set; } = [];
    public IReadOnlyList<Client> AvailableClients { get; private set; } = [];
    public IReadOnlyList<ClientUnit> AvailableClientUnits { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        // 1. Carrega lookups para filtros
        AvailableProducts = await _catalogRepository.GetAllProductsAsync();
        AvailableComponents = await _catalogRepository.GetAllComponentsAsync();
        AvailableRootCauses = await _caseResolutionService.GetRootCausesAsync();
        AvailableDepartments = await _departmentRepository.GetAllAsync();
        AvailableClients = await _clientRepository.GetAllAsync();

        if (ClientId.HasValue)
        {
            AvailableClientUnits = await _clientRepository.GetUnitsByClientIdAsync(ClientId.Value);
        }

        // Nomes para os chips
        string? prodName = AvailableProducts.FirstOrDefault(p => p.Id == ProductId)?.Name;
        string? compName = AvailableComponents.FirstOrDefault(c => c.Id == ComponentId)?.Name;
        string? rcName = AvailableRootCauses.FirstOrDefault(r => r.Id == RootCauseId)?.Name;
        string? deptName = AvailableDepartments.FirstOrDefault(d => d.Id == DepartmentId)?.Name;
        string? clientName = AvailableClients.FirstOrDefault(c => c.Id == ClientId)?.Name;
        string? unitName = AvailableClientUnits.FirstOrDefault(u => u.Id == ClientUnitId)?.Name;

        var filters = new SearchFilterCriteria(
            ProductId: ProductId,
            ProductName: prodName,
            ProductVersionId: ProductVersionId,
            ClientId: ClientId,
            ClientName: clientName,
            ClientUnitId: ClientUnitId,
            ClientUnitName: unitName,
            DepartmentId: DepartmentId,
            DepartmentName: deptName,
            TechnologyId: TechnologyId,
            ComponentId: ComponentId,
            ComponentName: compName,
            ErrorCode: ErrorCode,
            RootCauseId: RootCauseId,
            RootCauseName: rcName,
            Status: Status,
            Environment: Environment
        );

        long? userId = GetCurrentUserId();

        var command = new ExecuteSearchCommand(
            QueryText: Q,
            Filters: filters,
            ContextCaseId: ContextCaseId,
            SelectedType: SelectedType
        );

        Results = await _searchService.SearchAsync(command, userId);

        return Page();
    }

    public async Task<IActionResult> OnPostRecordInteractionAsync([FromBody] RecordResultInteractionCommand command)
    {
        if (command == null || command.QueryId <= 0) return BadRequest();

        var id = await _searchService.RecordInteractionAsync(command);
        return new JsonResult(new { success = true, interactionId = id });
    }

    public async Task<IActionResult> OnPostFeedbackAsync([FromBody] RecordFeedbackCommand command)
    {
        if (command == null || command.InteractionId <= 0) return BadRequest();

        await _searchService.RecordFeedbackAsync(command);
        return new JsonResult(new { success = true });
    }

    private long? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(idClaim, out var id) ? id : null;
    }
}
