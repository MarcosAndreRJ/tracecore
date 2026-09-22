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

// Gate único da página: quem gerencia o catálogo gerencia também os vínculos de
// componentes e integrações exibidos aqui (Fase 04 — decisão documentada no relatório).
[Authorize(Policy = "catalogo.gerenciar")]
public class DetailsModel : PageModel
{
    private readonly ICatalogService _catalogService;
    private readonly IProductTechnicalContextService _technicalContextService;
    private readonly IIntegrationService _integrationService;
    private readonly IIntegrationHealthCheckService _integrationHealthCheckService;
    private readonly IDepartmentService _departmentService;

    public DetailsModel(
        ICatalogService catalogService,
        IProductTechnicalContextService technicalContextService,
        IIntegrationService integrationService,
        IIntegrationHealthCheckService integrationHealthCheckService,
        IDepartmentService departmentService)
    {
        _catalogService = catalogService;
        _technicalContextService = technicalContextService;
        _integrationService = integrationService;
        _integrationHealthCheckService = integrationHealthCheckService;
        _departmentService = departmentService;
    }

    public Product? Product { get; private set; }
    public ProductInvestigationContextDto? Context { get; private set; }
    public System.Collections.Generic.IReadOnlyList<ProductExternalResearchDomainDto> AllowedDomainList { get; private set; } = System.Array.Empty<ProductExternalResearchDomainDto>();

    // Aba Versões — rastreabilidade de em qual versão um problema aparece (Bloco 7.A.2).
    public IReadOnlyList<ProductVersion> Versions { get; private set; } = [];

    // Aba Componentes (entidades completas — precisamos de Id/Status/ProductId para as ações).
    public IReadOnlyList<ComponentEntity> Components { get; private set; } = [];
    public IReadOnlyList<ComponentType> ComponentTypesList { get; private set; } = [];
    public IReadOnlyList<DepartmentDto> DepartmentsList { get; private set; } = [];

    // Aba Integrações (DTOs completos com histórico de runs).
    public IReadOnlyList<IntegrationDto> Integrations { get; private set; } = [];
    public IReadOnlyList<IntegrationDto> CandidateIntegrations { get; private set; } = [];
    public IReadOnlyList<IntegrationType> IntegrationTypesList { get; private set; } = [];

    public string[] SystemTypes { get; } = ProductTechnicalProfile.ValidSystemTypes;

    public string[] ValidResponsibilities { get; } = Integration.ValidResponsibilities;
    public string[] ValidHostingLocations { get; } = Integration.ValidHostingLocations;
    public string[] ValidDirections { get; } = Integration.ValidDirections;

    [BindProperty]
    public ProfileInput ProfileForm { get; set; } = new();

    [BindProperty]
    public string? TechnologiesCsv { get; set; }

    [BindProperty]
    public SourceInput NewSource { get; set; } = new();

    [BindProperty]
    public VersionInput NewVersion { get; set; } = new();

    [BindProperty]
    public ComponentInput NewComponent { get; set; } = new();

    [BindProperty]
    public ComponentInput EditComponent { get; set; } = new();

    [BindProperty]
    public IntegrationInput NewIntegration { get; set; } = new();

    [BindProperty]
    public IntegrationInput EditIntegration { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public record ProfileInput
    {
        public string? BusinessPurpose { get; set; }
        public string? ArchitectureSummary { get; set; }
        public string? SystemType { get; set; }
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

    public record VersionInput
    {
        public string VersionLabel { get; set; } = string.Empty;
        public DateTime? ReleasedAt { get; set; }
    }

    public record ComponentInput
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ComponentType { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public long? OwnerDepartmentId { get; set; }
        public string Status { get; set; } = "Active";
    }

    public record IntegrationInput
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IntegrationType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ContractNotes { get; set; }
        public long? OwnerDepartmentId { get; set; }
        public string? Responsibility { get; set; }
        public string? HostingLocation { get; set; }
        public string? Direction { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        Product = await _catalogService.GetProductByIdAsync(id);
        if (Product == null)
            return NotFound();

        await LoadPageAsync(id);

        if (Context?.TechnicalProfile != null)
        {
            var p = Context.TechnicalProfile;
            ProfileForm = new ProfileInput
            {
                BusinessPurpose = p.BusinessPurpose,
                ArchitectureSummary = p.ArchitectureSummary,
                SystemType = p.SystemType,
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

    private async Task LoadPageAsync(long productId)
    {
        Context = await _technicalContextService.GetInvestigationContextAsync(productId, null);
        var allDomains = await _technicalContextService.GetAllowedDomainsAsync(productId);
        AllowedDomainList = allDomains.Where(d => d.IsActive).ToList();

        Versions = (await _catalogService.GetVersionsByProductIdAsync(productId))
            .OrderByDescending(v => v.ReleasedAt ?? DateTime.MinValue)
            .ThenByDescending(v => v.Id)
            .ToList();

        Components = await _catalogService.GetAllComponentsAsync(productId);
        ComponentTypesList = await _catalogService.GetComponentTypesAsync(includeInactive: true);
        DepartmentsList = await _departmentService.GetAllDepartmentsAsync();

        var allIntegrations = await _integrationService.GetIntegrationsAsync();
        Integrations = allIntegrations.Where(i => i.ProductId == productId).OrderBy(i => i.Code).ToList();
        CandidateIntegrations = allIntegrations
            .Where(i => i.ProductId != productId)
            .OrderBy(i => i.Code)
            .ToList();
        IntegrationTypesList = await _integrationService.GetIntegrationTypesAsync(includeInactive: true);
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
                    ProfileForm.ExternalResearchPolicy,
                    SystemType: ProfileForm.SystemType),
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

    // --- Aba Versões ---------------------------------------------------------

    public async Task<IActionResult> OnPostAddVersionAsync(long id)
    {
        if (string.IsNullOrWhiteSpace(NewVersion.VersionLabel))
        {
            ErrorMessage = "O rótulo da versão é obrigatório.";
            return RedirectToPage(new { id, tab = "versions" });
        }

        try
        {
            await _catalogService.CreateVersionAsync(
                id,
                NewVersion.VersionLabel.Trim(),
                NewVersion.ReleasedAt,
                GetCurrentUserId());

            SuccessMessage = $"Versão '{NewVersion.VersionLabel}' registrada para este sistema.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                ? $"Este sistema já tem uma versão '{NewVersion.VersionLabel}' cadastrada."
                : $"Erro ao registrar versão: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

    // --- Aba Componentes ---------------------------------------------------

    public async Task<IActionResult> OnPostAddComponentAsync(long id)
    {
        if (string.IsNullOrWhiteSpace(NewComponent.Name))
        {
            ErrorMessage = "O nome do componente é obrigatório.";
            return RedirectToPage(new { id, tab = "components" });
        }

        try
        {
            await _catalogService.CreateComponentAsync(
                NewComponent.Name,
                NewComponent.ComponentType,
                productId: id,
                code: NewComponent.Code,
                description: NewComponent.Description,
                ownerDepartmentId: NewComponent.OwnerDepartmentId,
                currentUserId: GetCurrentUserId());

            SuccessMessage = $"Componente '{NewComponent.Name}' adicionado ao sistema.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao adicionar componente: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "components" });
    }

    public async Task<IActionResult> OnPostEditComponentAsync(long id, long componentId)
    {
        try
        {
            var existing = await _catalogService.GetComponentByIdAsync(componentId);
            if (existing == null)
                throw new KeyNotFoundException($"Componente com ID {componentId} não encontrado.");

            await _catalogService.UpdateComponentAsync(
                componentId,
                EditComponent.Name,
                EditComponent.ComponentType,
                productId: existing.ProductId,
                code: EditComponent.Code,
                description: EditComponent.Description,
                ownerDepartmentId: EditComponent.OwnerDepartmentId,
                status: EditComponent.Status,
                currentUserId: GetCurrentUserId());

            SuccessMessage = $"Componente '{EditComponent.Name}' atualizado.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar componente: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "components" });
    }

    public async Task<IActionResult> OnPostDeactivateComponentAsync(long id, long componentId)
    {
        try
        {
            var existing = await _catalogService.GetComponentByIdAsync(componentId);
            if (existing == null)
                throw new KeyNotFoundException($"Componente com ID {componentId} não encontrado.");

            if (existing.Status != "Inactive")
            {
                await _catalogService.UpdateComponentAsync(
                    componentId,
                    existing.Name,
                    existing.ComponentType,
                    existing.ProductId,
                    existing.Code,
                    existing.Description,
                    existing.OwnerDepartmentId,
                    status: "Inactive",
                    currentUserId: GetCurrentUserId());
            }

            SuccessMessage = $"Componente '{existing.Name}' inativado.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao inativar componente: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "components" });
    }

    public async Task<IActionResult> OnPostUnlinkComponentAsync(long id, long componentId)
    {
        try
        {
            var existing = await _catalogService.GetComponentByIdAsync(componentId);
            if (existing == null)
                throw new KeyNotFoundException($"Componente com ID {componentId} não encontrado.");

            await _catalogService.UpdateComponentAsync(
                componentId,
                existing.Name,
                existing.ComponentType,
                productId: null,
                existing.Code,
                existing.Description,
                existing.OwnerDepartmentId,
                status: existing.Status,
                currentUserId: GetCurrentUserId());

            SuccessMessage = $"Componente '{existing.Name}' desvinculado do sistema.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao desvincular componente: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "components" });
    }

    // --- Aba Integrações ---------------------------------------------------

    public async Task<IActionResult> OnPostAddIntegrationAsync(long id)
    {
        if (string.IsNullOrWhiteSpace(NewIntegration.Name) || string.IsNullOrWhiteSpace(NewIntegration.Code))
        {
            ErrorMessage = "Nome e código da integração são obrigatórios.";
            return RedirectToPage(new { id, tab = "integrations" });
        }

        try
        {
            await _integrationService.CreateIntegrationAsync(
                new CreateIntegrationCommand(
                    Code: NewIntegration.Code,
                    Name: NewIntegration.Name,
                    IntegrationType: NewIntegration.IntegrationType,
                    TargetSystemDescription: NewIntegration.Description,
                    OwnerDepartmentId: NewIntegration.OwnerDepartmentId,
                    ContractNotes: NewIntegration.ContractNotes,
                    CreatedBy: GetCurrentUserId(),
                    ProductId: id,
                    Responsibility: NewIntegration.Responsibility,
                    HostingLocation: NewIntegration.HostingLocation,
                    Direction: NewIntegration.Direction));

            SuccessMessage = $"Integração '{NewIntegration.Name}' criada e vinculada ao sistema.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao criar integração: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "integrations" });
    }

    public async Task<IActionResult> OnPostEditIntegrationAsync(long id, long integrationId)
    {
        try
        {
            var existing = await _integrationService.GetIntegrationByIdAsync(integrationId);
            if (existing == null)
                throw new KeyNotFoundException($"Integração com ID {integrationId} não encontrada.");

            await _integrationService.UpdateIntegrationAsync(
                new UpdateIntegrationCommand(
                    Id: integrationId,
                    Code: EditIntegration.Code,
                    Name: EditIntegration.Name,
                    IntegrationType: EditIntegration.IntegrationType,
                    ProductId: existing.ProductId,
                    TargetSystemDescription: EditIntegration.Description,
                    OwnerDepartmentId: EditIntegration.OwnerDepartmentId,
                    ContractNotes: EditIntegration.ContractNotes,
                    Responsibility: EditIntegration.Responsibility,
                    HostingLocation: EditIntegration.HostingLocation,
                    Direction: EditIntegration.Direction),
                GetCurrentUserId());

            SuccessMessage = $"Integração '{EditIntegration.Name}' atualizada.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar integração: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "integrations" });
    }

    public async Task<IActionResult> OnPostLinkExistingIntegrationAsync(long id, long integrationId)
    {
        try
        {
            await _integrationService.LinkIntegrationToProductAsync(integrationId, id, GetCurrentUserId());
            SuccessMessage = $"Integração vinculada ao sistema '{Product?.Name}'.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao vincular integração: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "integrations" });
    }

    public async Task<IActionResult> OnPostUnlinkIntegrationAsync(long id, long integrationId)
    {
        try
        {
            await _integrationService.UnlinkIntegrationFromProductAsync(integrationId, GetCurrentUserId());
            SuccessMessage = $"Integração desvinculada do sistema '{Product?.Name}'.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao desvincular integração: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "integrations" });
    }

    public async Task<IActionResult> OnPostTestIntegrationHealthCheckAsync(long id, long integrationId)
    {
        try
        {
            var run = await _integrationHealthCheckService.ExecuteHealthCheckAsync(integrationId);
            SuccessMessage = run.Status == "Success"
                ? "Health-check executado com sucesso."
                : $"Health-check falhou: {run.ErrorMessage}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao executar health-check: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "integrations" });
    }

    private long? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(idClaim, out var id) ? id : null;
    }
}