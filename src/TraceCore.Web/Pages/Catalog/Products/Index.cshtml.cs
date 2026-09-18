using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;

namespace TraceCore.Web.Pages.Catalog.Products;

[Authorize(Policy = "catalogo.gerenciar")]
public class IndexModel : PageModel
{
    private readonly ICatalogService _catalogService;

    public IndexModel(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public IReadOnlyList<Product> ProductsList { get; private set; } = [];

    [BindProperty]
    public CreateProductInput NewProduct { get; set; } = new();

    [BindProperty]
    public EditProductInput EditProduct { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public record CreateProductInput
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public bool IsExternal { get; set; } = false;
    }

    public record EditProductInput
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsExternal { get; set; } = false;
    }

    public async Task OnGetAsync()
    {
        ProductsList = await _catalogService.GetAllProductsAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProduct.Name))
        {
            ErrorMessage = "O nome do produto é obrigatório.";
            return RedirectToPage();
        }

        try
        {
            await _catalogService.CreateProductAsync(
                NewProduct.Name,
                NewProduct.Code,
                NewProduct.Description,
                NewProduct.IsExternal);

            SuccessMessage = $"Produto '{NewProduct.Name}' cadastrado com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao cadastrar produto: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (EditProduct.Id <= 0 || string.IsNullOrWhiteSpace(EditProduct.Name))
        {
            ErrorMessage = "Dados inválidos para edição do produto.";
            return RedirectToPage();
        }

        try
        {
            await _catalogService.UpdateProductAsync(
                EditProduct.Id,
                EditProduct.Name,
                EditProduct.Code,
                EditProduct.Description,
                EditProduct.Status,
                EditProduct.IsExternal);

            SuccessMessage = $"Produto '{EditProduct.Name}' atualizado com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar produto: {ex.Message}";
        }

        return RedirectToPage();
    }
}
