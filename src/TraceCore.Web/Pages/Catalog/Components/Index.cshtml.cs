using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;

namespace TraceCore.Web.Pages.Catalog.Components;

[Authorize(Policy = "catalogo.gerenciar")]
public class IndexModel : PageModel
{
    private readonly ICatalogService _catalogService;
    private readonly IDepartmentService _departmentService;

    public IndexModel(ICatalogService catalogService, IDepartmentService departmentService)
    {
        _catalogService = catalogService;
        _departmentService = departmentService;
    }

    public IReadOnlyList<ComponentEntity> ComponentsList { get; private set; } = [];
    public IReadOnlyList<Product> ProductsList { get; private set; } = [];
    public IReadOnlyList<DepartmentDto> DepartmentsList { get; private set; } = [];
    public IReadOnlyList<ComponentDependency> DependenciesList { get; private set; } = [];
    public IReadOnlyList<ComponentOwner> OwnersList { get; private set; } = [];

    [BindProperty]
    public CreateComponentInput NewComponent { get; set; } = new();

    [BindProperty]
    public CreateDependencyInput NewDependency { get; set; } = new();

    [BindProperty]
    public CreateOwnerInput NewOwner { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public record CreateComponentInput
    {
        public string Name { get; set; } = string.Empty;
        public string ComponentType { get; set; } = "Service";
        public long? ProductId { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public long? OwnerDepartmentId { get; set; }
    }

    public record CreateDependencyInput
    {
        public long SourceComponentId { get; set; }
        public long TargetComponentId { get; set; }
        public string DependencyType { get; set; } = "Synchronous";
        public string Criticality { get; set; } = "Medium";
        public string? Description { get; set; }
    }

    public record CreateOwnerInput
    {
        public long ComponentId { get; set; }
        public long DepartmentId { get; set; }
        public string OwnershipRole { get; set; } = "Primary";
    }

    public async Task OnGetAsync()
    {
        ComponentsList = await _catalogService.GetAllComponentsAsync();
        ProductsList = await _catalogService.GetAllProductsAsync();
        DepartmentsList = await _departmentService.GetAllDepartmentsAsync();
        DependenciesList = await _catalogService.GetComponentDependenciesAsync();
        OwnersList = await _catalogService.GetComponentOwnersAsync();
    }

    public async Task<IActionResult> OnPostCreateComponentAsync()
    {
        if (string.IsNullOrWhiteSpace(NewComponent.Name) || string.IsNullOrWhiteSpace(NewComponent.ComponentType))
        {
            ErrorMessage = "Nome e Tipo do componente são obrigatórios.";
            return RedirectToPage();
        }

        try
        {
            await _catalogService.CreateComponentAsync(
                NewComponent.Name,
                NewComponent.ComponentType,
                NewComponent.ProductId,
                NewComponent.Code,
                NewComponent.Description,
                NewComponent.OwnerDepartmentId);

            SuccessMessage = $"Componente '{NewComponent.Name}' cadastrado com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao cadastrar componente: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateDependencyAsync()
    {
        if (NewDependency.SourceComponentId <= 0 || NewDependency.TargetComponentId <= 0)
        {
            ErrorMessage = "Componente de origem e destino devem ser selecionados.";
            return RedirectToPage();
        }

        if (NewDependency.SourceComponentId == NewDependency.TargetComponentId)
        {
            ErrorMessage = "Regra de Domínio: Um componente não pode depender de si mesmo.";
            return RedirectToPage();
        }

        try
        {
            await _catalogService.AddComponentDependencyAsync(
                NewDependency.SourceComponentId,
                NewDependency.TargetComponentId,
                NewDependency.DependencyType,
                NewDependency.Criticality,
                NewDependency.Description);

            SuccessMessage = "Dependência técnica registrada com sucesso no catálogo.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao criar dependência: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteDependencyAsync(long dependencyId)
    {
        try
        {
            var success = await _catalogService.DeleteComponentDependencyAsync(dependencyId);
            if (success)
                SuccessMessage = "Dependência técnica removida com sucesso.";
            else
                ErrorMessage = "Dependência não encontrada.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao remover dependência: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateOwnerAsync()
    {
        if (NewOwner.ComponentId <= 0 || NewOwner.DepartmentId <= 0)
        {
            ErrorMessage = "Componente e Departamento responsável devem ser informados.";
            return RedirectToPage();
        }

        try
        {
            await _catalogService.AddComponentOwnerAsync(
                NewOwner.ComponentId,
                NewOwner.DepartmentId,
                NewOwner.OwnershipRole);

            SuccessMessage = "Vínculo de departamento responsável registrado com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao vincular departamento: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteOwnerAsync(long ownerId)
    {
        try
        {
            var success = await _catalogService.DeleteComponentOwnerAsync(ownerId);
            if (success)
                SuccessMessage = "Responsabilidade de departamento removida com sucesso.";
            else
                ErrorMessage = "Registro de ownership não encontrado.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao remover responsabilidade: {ex.Message}";
        }

        return RedirectToPage();
    }
}
