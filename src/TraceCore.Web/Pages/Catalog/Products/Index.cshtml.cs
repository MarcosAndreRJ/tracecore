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

namespace TraceCore.Web.Pages.Catalog.Products;

[Authorize(Policy = "catalogo.gerenciar")]
public class IndexModel : PageModel
{
    private readonly ICatalogService _catalogService;
    private readonly IProductTechnicalContextService _technicalContextService;

    public IndexModel(
        ICatalogService catalogService,
        IProductTechnicalContextService technicalContextService)
    {
        _catalogService = catalogService;
        _technicalContextService = technicalContextService;
    }

    public IReadOnlyList<Product> ProductsList { get; private set; } = [];

    // Total sem filtro, para o contador "X produto(s) no portfólio" continuar fazendo
    // sentido mesmo com um filtro ativo reduzindo a lista exibida.
    public int TotalProductsCount { get; private set; }

    // Perfil técnico por produto carregado em lote na mesma submissão do GET
    // (1 query, sem N+1) — alimenta o modal "Editar Sistema".
    public IReadOnlyDictionary<long, ProductTechnicalProfileDto> ProfilesByProductId { get; private set; } = new Dictionary<long, ProductTechnicalProfileDto>();

    public string[] SystemTypes { get; } = ProductTechnicalProfile.ValidSystemTypes;

    // Filtros da listagem (Bloco 7.A.2)
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Origin { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty]
    public CreateProductInput NewProduct { get; set; } = new();

    [BindProperty]
    public EditProductInput EditProduct { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    // Não é [TempData]: erros de criação/edição agora são exibidos re-renderizando a
    // própria página (sem redirect, para preservar o que o usuário digitou — ver
    // ReopenNewProductModal/ReopenEditProductId), então só precisam durar esta resposta.
    // Se fosse [TempData], o valor "vazaria" e reapareceria na próxima navegação.
    public string? ErrorMessage { get; set; }

    // Quando um cadastro/edição falha, a página é re-renderizada (sem redirect) para
    // preservar o que o usuário digitou — estas flags dizem à view qual modal reabrir.
    public bool ReopenNewProductModal { get; set; }
    public long? ReopenEditProductId { get; set; }

    public record CreateProductInput
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public bool IsExternal { get; set; } = false;

        // Contexto técnico básico (cadastro + perfil inicial na mesma submissão).
        public string? SystemType { get; set; }
        public string? Technology { get; set; }
        public string? PrimaryDatabase { get; set; }
        public string? HostingModel { get; set; }
        public string? BusinessPurpose { get; set; }
        public string? ArchitectureSummary { get; set; }
    }

    public record EditProductInput
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsExternal { get; set; } = false;

        // Contexto técnico básico (espelho do cadastro).
        public string? SystemType { get; set; }
        public string? Technology { get; set; }
        public string? PrimaryDatabase { get; set; }
        public string? HostingModel { get; set; }
        public string? BusinessPurpose { get; set; }
        public string? ArchitectureSummary { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var allProducts = await _catalogService.GetAllProductsAsync();
        TotalProductsCount = allProducts.Count;

        var query = allProducts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(p.Code) && p.Code.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.Description) && p.Description.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(Origin))
        {
            var wantExternal = Origin.Equals("External", StringComparison.OrdinalIgnoreCase);
            query = query.Where(p => p.IsExternal == wantExternal);
        }

        if (!string.IsNullOrWhiteSpace(Status))
        {
            query = query.Where(p => p.Status.Equals(Status, StringComparison.OrdinalIgnoreCase));
        }

        ProductsList = query.ToList();

        if (ProductsList.Count > 0)
        {
            ProfilesByProductId = await _technicalContextService.GetTechnicalProfilesForAsync(
                ProductsList.Select(p => p.Id).ToList());
        }
    }

    // Mensagens do MySQL/infra não devem chegar cruas ao usuário; traduz os casos
    // previsíveis (chave duplicada) e mantém o resto como está.
    private static string DescribeError(string action, string? code, Exception ex)
    {
        if (ex.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(code)
                ? "Já existe um sistema cadastrado com esses dados. Verifique o código informado."
                : $"Já existe um sistema cadastrado com o código '{code}'. Escolha outro código.";
        }

        return $"Erro ao {action} produto: {ex.Message}";
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProduct.Name))
        {
            ErrorMessage = "O nome do produto é obrigatório.";
            ReopenNewProductModal = true;
            await LoadAsync();
            return Page();
        }

        try
        {
            var productId = await _catalogService.CreateProductAsync(
                NewProduct.Name,
                NewProduct.Code,
                NewProduct.Description,
                NewProduct.IsExternal,
                GetCurrentUserId());

            // Perfil técnico inicial: sempre criado na mesma submissão do cadastro
            // (campos opcionais — perfil vazio é válido).
            await _technicalContextService.UpsertTechnicalProfileAsync(
                new UpsertProductTechnicalProfileCommand(
                    productId,
                    BusinessPurpose: NewProduct.BusinessPurpose,
                    ArchitectureSummary: NewProduct.ArchitectureSummary,
                    FrontendStack: null,
                    BackendStack: NewProduct.Technology,
                    PrimaryDatabase: NewProduct.PrimaryDatabase,
                    RuntimePlatform: null,
                    HostingModel: NewProduct.HostingModel,
                    AuthenticationModel: null,
                    ObservabilityStack: null,
                    DeploymentModel: null,
                    Vendor: null,
                    SupportNotes: null,
                    KnownConstraints: null,
                    InvestigationNotes: null,
                    ExternalResearchPolicy: "Disabled",
                    SystemType: NewProduct.SystemType),
                GetCurrentUserId());

            SuccessMessage = $"Sistema '{NewProduct.Name}' cadastrado com sucesso. Continue por aqui para adicionar Componentes e Integrações.";
            return RedirectToPage("/Catalog/Products/Details", new { id = productId });
        }
        catch (Exception ex)
        {
            ErrorMessage = DescribeError("cadastrar", NewProduct.Code, ex);
        }

        ReopenNewProductModal = true;
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (EditProduct.Id <= 0 || string.IsNullOrWhiteSpace(EditProduct.Name))
        {
            ErrorMessage = "Dados inválidos para edição do produto.";
            ReopenEditProductId = EditProduct.Id > 0 ? EditProduct.Id : null;
            await LoadAsync();
            return Page();
        }

        try
        {
            await _catalogService.UpdateProductAsync(
                EditProduct.Id,
                EditProduct.Name,
                EditProduct.Code,
                EditProduct.Description,
                EditProduct.Status,
                EditProduct.IsExternal,
                GetCurrentUserId());

            var existingProfile = await _technicalContextService.GetTechnicalProfileAsync(EditProduct.Id);
            await _technicalContextService.UpsertTechnicalProfileAsync(
                new UpsertProductTechnicalProfileCommand(
                    EditProduct.Id,
                    BusinessPurpose: EditProduct.BusinessPurpose,
                    ArchitectureSummary: EditProduct.ArchitectureSummary,
                    FrontendStack: existingProfile?.FrontendStack,
                    BackendStack: EditProduct.Technology,
                    PrimaryDatabase: EditProduct.PrimaryDatabase,
                    RuntimePlatform: existingProfile?.RuntimePlatform,
                    HostingModel: EditProduct.HostingModel,
                    AuthenticationModel: existingProfile?.AuthenticationModel,
                    ObservabilityStack: existingProfile?.ObservabilityStack,
                    DeploymentModel: existingProfile?.DeploymentModel,
                    Vendor: existingProfile?.Vendor,
                    SupportNotes: existingProfile?.SupportNotes,
                    KnownConstraints: existingProfile?.KnownConstraints,
                    InvestigationNotes: existingProfile?.InvestigationNotes,
                    ExternalResearchPolicy: existingProfile?.ExternalResearchPolicy ?? "Disabled",
                    SystemType: EditProduct.SystemType),
                GetCurrentUserId());

            SuccessMessage = $"Produto '{EditProduct.Name}' atualizado com sucesso.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = DescribeError("atualizar", EditProduct.Code, ex);
        }

        ReopenEditProductId = EditProduct.Id;
        await LoadAsync();
        return Page();
    }

    private long? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, out var id) ? id : null;
    }
}