using System;
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
public class DetailsModel : PageModel
{
    private readonly ICatalogService _catalogService;
    private readonly IProductTechnicalContextService _technicalContextService;

    public DetailsModel(ICatalogService catalogService, IProductTechnicalContextService technicalContextService)
    {
        _catalogService = catalogService;
        _technicalContextService = technicalContextService;
    }

    public Product? Product { get; private set; }
    public ProductInvestigationContextDto? Context { get; private set; }
    public System.Collections.Generic.IReadOnlyList<ProductExternalResearchDomainDto> AllowedDomainList { get; private set; } = System.Array.Empty<ProductExternalResearchDomainDto>();

    [BindProperty]
    public ProfileInput ProfileForm { get; set; } = new();

    [BindProperty]
    public string? TechnologiesCsv { get; set; }

    [BindProperty]
    public SourceInput NewSource { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public record ProfileInput
    {
        public string? BusinessPurpose { get; set; }
        public string? ArchitectureSummary { get; set; }
        public string? FrontendStack { get; set; }
        public string? BackendStack { get; set; }
        public string? PrimaryDatabase { get; set; }
        public string? RuntimePlatform { get; set; }
        public string? HostingModel { get; set; }
        public string? AuthenticationModel { get; set; }
        public string? ObservabilityStack { get; set; }
        public string? DeploymentModel { get; set; }
        public string? Vendor { get; set; }
        public string? SupportNotes { get; set; }
        public string? KnownConstraints { get; set; }
        public string? InvestigationNotes { get; set; }
        public string ExternalResearchPolicy { get; set; } = "Disabled";
    }

    public record SourceInput
    {
        public string Name { get; set; } = string.Empty;
        public string SourceType { get; set; } = "Other";
        public string? Url { get; set; }
        public string? Description { get; set; }
        public string TrustLevel { get; set; } = "Reference";
    }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        Product = await _catalogService.GetProductByIdAsync(id);
        if (Product == null)
            return NotFound();

        Context = await _technicalContextService.GetInvestigationContextAsync(id, null);
        var allDomains = await _technicalContextService.GetAllowedDomainsAsync(id);
        AllowedDomainList = allDomains.Where(d => d.IsActive).ToList();

        if (Context?.TechnicalProfile != null)
        {
            var p = Context.TechnicalProfile;
            ProfileForm = new ProfileInput
            {
                BusinessPurpose = p.BusinessPurpose,
                ArchitectureSummary = p.ArchitectureSummary,
                FrontendStack = p.FrontendStack,
                BackendStack = p.BackendStack,
                PrimaryDatabase = p.PrimaryDatabase,
                RuntimePlatform = p.RuntimePlatform,
                HostingModel = p.HostingModel,
                AuthenticationModel = p.AuthenticationModel,
                ObservabilityStack = p.ObservabilityStack,
                DeploymentModel = p.DeploymentModel,
                Vendor = p.Vendor,
                SupportNotes = p.SupportNotes,
                KnownConstraints = p.KnownConstraints,
                InvestigationNotes = p.InvestigationNotes,
                ExternalResearchPolicy = p.ExternalResearchPolicy
            };
        }

        TechnologiesCsv = string.Join(", ", Context?.Technologies ?? Array.Empty<string>());

        return Page();
    }

    public async Task<IActionResult> OnPostSaveProfileAsync(long id)
    {
        try
        {
            await _technicalContextService.UpsertTechnicalProfileAsync(
                new UpsertProductTechnicalProfileCommand(
                    id,
                    ProfileForm.BusinessPurpose,
                    ProfileForm.ArchitectureSummary,
                    ProfileForm.FrontendStack,
                    ProfileForm.BackendStack,
                    ProfileForm.PrimaryDatabase,
                    ProfileForm.RuntimePlatform,
                    ProfileForm.HostingModel,
                    ProfileForm.AuthenticationModel,
                    ProfileForm.ObservabilityStack,
                    ProfileForm.DeploymentModel,
                    ProfileForm.Vendor,
                    ProfileForm.SupportNotes,
                    ProfileForm.KnownConstraints,
                    ProfileForm.InvestigationNotes,
                    ProfileForm.ExternalResearchPolicy),
                GetCurrentUserId());

            SuccessMessage = "Contexto técnico salvo com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao salvar contexto técnico: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "technical" });
    }

    public async Task<IActionResult> OnPostSetTechnologiesAsync(long id)
    {
        try
        {
            var names = (TechnologiesCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            await _technicalContextService.SetProductTechnologiesAsync(id, names, GetCurrentUserId());
            SuccessMessage = "Tecnologias atualizadas com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar tecnologias: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "technical" });
    }

    public async Task<IActionResult> OnPostAddSourceAsync(long id)
    {
        try
        {
            await _technicalContextService.AddTechnicalSourceAsync(
                new CreateProductTechnicalSourceCommand(id, NewSource.Name, NewSource.SourceType, NewSource.Url, NewSource.Description, NewSource.TrustLevel),
                GetCurrentUserId());

            SuccessMessage = "Fonte técnica adicionada com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao adicionar fonte técnica: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "sources" });
    }

    public async Task<IActionResult> OnPostDisableSourceAsync(long id, long sourceId)
    {
        try
        {
            await _technicalContextService.DisableTechnicalSourceAsync(sourceId, GetCurrentUserId());
            SuccessMessage = "Fonte técnica desativada.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao desativar fonte técnica: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "sources" });
    }

    public async Task<IActionResult> OnPostAddDomainAsync(long id, string domain, string? description)
    {
        try
        {
            await _technicalContextService.AddAllowedDomainAsync(id, domain, description, GetCurrentUserId());
            SuccessMessage = "Domínio adicionado à allowlist.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao adicionar domínio: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "research" });
    }

    public async Task<IActionResult> OnPostRemoveDomainAsync(long id, long domainId)
    {
        try
        {
            await _technicalContextService.RemoveAllowedDomainAsync(domainId, GetCurrentUserId());
            SuccessMessage = "Domínio removido da allowlist.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao remover domínio: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "research" });
    }

    private long? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(idClaim, out var id) ? id : null;
    }
}
