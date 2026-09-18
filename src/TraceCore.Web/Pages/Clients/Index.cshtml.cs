using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Web.Pages.Clients;

[Authorize(Policy = "cliente.gerenciar")]
public class IndexModel : PageModel
{
    private readonly IClientService _clientService;
    private readonly ICatalogRepository _catalogRepository;

    public IndexModel(IClientService clientService, ICatalogRepository catalogRepository)
    {
        _clientService = clientService;
        _catalogRepository = catalogRepository;
    }

    public IReadOnlyList<ClientDto> ClientsList { get; private set; } = [];
    public ClientDetailsDto? SelectedClient { get; private set; }

    public IReadOnlyList<Product> AvailableProducts { get; private set; } = [];
    public IReadOnlyList<EnvironmentEntity> AvailableEnvironments { get; private set; } = [];

    [BindProperty]
    public CreateClientRequest CreateInput { get; set; } = new(string.Empty);

    [BindProperty]
    public UpdateClientRequest UpdateInput { get; set; } = new(string.Empty);

    [BindProperty]
    public long EditClientId { get; set; }

    [BindProperty]
    public CreateClientUnitRequest UnitInput { get; set; } = new(string.Empty, string.Empty);

    [BindProperty]
    public long UnitClientId { get; set; }

    [BindProperty]
    public CreateTechnicalContextRequest ContextInput { get; set; } = new(0);

    [BindProperty]
    public long ContextClientId { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(long? selectedId = null)
    {
        await LoadDataAsync(selectedId);
    }

    public async Task<IActionResult> OnPostCreateClientAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            var id = await _clientService.CreateClientAsync(CreateInput, userId);
            SuccessMessage = $"Cliente #{id} cadastrado com sucesso!";
            return RedirectToPage(new { selectedId = id });
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (ConflictException ex)
        {
            ErrorMessage = ex.Message;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateClientAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            await _clientService.UpdateClientAsync(EditClientId, UpdateInput, userId);
            SuccessMessage = "Cliente atualizado com sucesso!";
            return RedirectToPage(new { selectedId = EditClientId });
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (ConflictException ex)
        {
            ErrorMessage = ex.Message;
        }

        await LoadDataAsync(EditClientId);
        return Page();
    }

    public async Task<IActionResult> OnPostAddUnitAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            await _clientService.AddUnitAsync(UnitClientId, UnitInput, userId);
            SuccessMessage = "Unidade cadastrada com sucesso!";
            return RedirectToPage(new { selectedId = UnitClientId });
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = ex.Message;
        }

        await LoadDataAsync(UnitClientId);
        return Page();
    }

    public async Task<IActionResult> OnPostAddContextAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            await _clientService.AddTechnicalContextAsync(ContextClientId, ContextInput, userId);
            SuccessMessage = "Contexto técnico vinculado com sucesso!";
            return RedirectToPage(new { selectedId = ContextClientId });
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = ex.Message;
        }

        await LoadDataAsync(ContextClientId);
        return Page();
    }

    public async Task<IActionResult> OnGetProductVersionsAsync(long productId)
    {
        var versions = await _catalogRepository.GetVersionsByProductIdAsync(productId);
        return new JsonResult(versions);
    }

    private async Task LoadDataAsync(long? selectedId = null)
    {
        ClientsList = await _clientService.GetAllClientsAsync();
        AvailableProducts = await _catalogRepository.GetAllProductsAsync();
        AvailableEnvironments = await _catalogRepository.GetAllEnvironmentsAsync();

        if (selectedId.HasValue)
        {
            SelectedClient = await _clientService.GetClientDetailsAsync(selectedId.Value);
        }
        else if (ClientsList.Count > 0)
        {
            SelectedClient = await _clientService.GetClientDetailsAsync(ClientsList[0].Id);
        }
    }

    private long? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
    }
}
