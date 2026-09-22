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

namespace TraceCore.Web.Pages.Catalog.Products;

// Gate único da página: quem gerencia o catálogo gerencia também os vínculos de
// componentes e integrações exibidos aqui (Fase 04 — decisão documentada no relatório).
[Authorize(Policy = "catalogo.gerenciar")]
public class DetailsModel : PageModel
{
    private readonly ICatalogService _catalogService;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IProductTechnicalContextService _technicalContextService;
    private readonly IIntegrationService _integrationService;
    private readonly IIntegrationHealthCheckService _integrationHealthCheckService;
    private readonly IDepartmentService _departmentService;
    private readonly IVersionManagementService _versionManagementService;
    private readonly IClientService _clientService;
    private readonly ICaseService _caseService;

    public DetailsModel(
        ICatalogService catalogService,
        ICatalogRepository catalogRepository,
        IProductTechnicalContextService technicalContextService,
        IIntegrationService integrationService,
        IIntegrationHealthCheckService integrationHealthCheckService,
        IDepartmentService departmentService,
        IVersionManagementService versionManagementService,
        IClientService clientService,
        ICaseService caseService)
    {
        _catalogService = catalogService;
        _catalogRepository = catalogRepository;
        _technicalContextService = technicalContextService;
        _integrationService = integrationService;
        _integrationHealthCheckService = integrationHealthCheckService;
        _departmentService = departmentService;
        _versionManagementService = versionManagementService;
        _clientService = clientService;
        _caseService = caseService;
    }

    public Product? Product { get; private set; }
    public ProductInvestigationContextDto? Context { get; private set; }
    public System.Collections.Generic.IReadOnlyList<ProductExternalResearchDomainDto> AllowedDomainList { get; private set; } = System.Array.Empty<ProductExternalResearchDomainDto>();

    // Aba Versões — rastreabilidade de em qual versão um problema aparece (Bloco 7.A.2).
    public IReadOnlyList<ProductVersion> Versions { get; private set; } = [];

    // Fase 1 & 2 (Versionamento Inteligente): itens de release e destinação por versão.
    public IReadOnlyDictionary<long, IReadOnlyList<ProductVersionChangeDto>> ChangesByVersion { get; private set; } = new System.Collections.Generic.Dictionary<long, IReadOnlyList<ProductVersionChangeDto>>();
    public IReadOnlyDictionary<long, IReadOnlyList<ProductVersionAssignmentDto>> AssignmentsByVersion { get; private set; } = new System.Collections.Generic.Dictionary<long, IReadOnlyList<ProductVersionAssignmentDto>>();
    public IReadOnlyDictionary<long, VersionIndicatorsDto> IndicatorsByVersion { get; private set; } = new System.Collections.Generic.Dictionary<long, VersionIndicatorsDto>();
    public IReadOnlyDictionary<long, IReadOnlyList<VersionLinkedCaseDetailDto>> LinkedCasesByVersion { get; private set; } = new System.Collections.Generic.Dictionary<long, IReadOnlyList<VersionLinkedCaseDetailDto>>();
    public IReadOnlyDictionary<long, FixRecurrenceObservationDto> RecurrenceByChangeId { get; private set; } = new System.Collections.Generic.Dictionary<long, FixRecurrenceObservationDto>();
    public IReadOnlyDictionary<long, int> CasesCountByVersion { get; private set; } = new System.Collections.Generic.Dictionary<long, int>();
    public IReadOnlyList<ClientDto> ClientsForRollout { get; private set; } = [];
    public IReadOnlyDictionary<long, IReadOnlyList<ClientUnitDto>> UnitsByClient { get; private set; } = new System.Collections.Generic.Dictionary<long, IReadOnlyList<ClientUnitDto>>();
    public IReadOnlyDictionary<long, IReadOnlyList<ClientTechnicalContextDto>> ClientContextsMap { get; private set; } = new System.Collections.Generic.Dictionary<long, IReadOnlyList<ClientTechnicalContextDto>>();
    public IReadOnlyList<EnvironmentEntity> AvailableEnvironments { get; private set; } = [];

    [BindProperty]
    public List<long> SelectedClientIdsForRollout { get; set; } = new();

    // Aba Componentes (entidades completas — precisamos de Id/Status/ProductId para as ações).
    public IReadOnlyList<ComponentEntity> Components { get; private set; } = [];
    public IReadOnlyList<ComponentType> ComponentTypesList { get; private set; } = [];
    public IReadOnlyList<DepartmentDto> DepartmentsList { get; private set; } = [];

    // Aba Integrações (DTOs completos com histórico de runs).
    public IReadOnlyList<IntegrationDto> Integrations { get; private set; } = [];
    public IReadOnlyList<IntegrationDto> CandidateIntegrations { get; private set; } = [];
    public IReadOnlyList<IntegrationType> IntegrationTypesList { get; private set; } = [];

    public string[] SystemTypes { get; } = ProductTechnicalProfile.ValidSystemTypes;

    public string[] ValidChangeTypes { get; } = ProductVersionChange.ValidChangeTypes;
    public string[] ValidRelationTypes { get; } = ProductVersionChangeCase.ValidRelationTypes;
    public string[] ValidAssignmentStatuses { get; } = ProductVersionAssignment.ValidStatuses;

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

    // Fase 1 (Versionamento Inteligente) — formulários da aba Versões.
    [BindProperty]
    public ChangeInput NewChange { get; set; } = new();

    [BindProperty]
    public ChangeInput EditChange { get; set; } = new();

    [BindProperty]
    public CaseLinkInput NewCaseLink { get; set; } = new();

    [BindProperty]
    public AssignmentInput NewAssignment { get; set; } = new();

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

    public record ChangeInput
    {
        public long VersionId { get; set; }
        public long Id { get; set; }
        public string ChangeType { get; set; } = "Improvement";
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long? ComponentId { get; set; }
        public string? ErrorCode { get; set; }
        public List<long> SelectedCaseIdsToLink { get; set; } = new();
    }

    public record CaseLinkInput
    {
        public long ChangeId { get; set; }
        public ulong CaseNumber { get; set; }
        public string RelationType { get; set; } = "FixedBy";
    }

    public record AssignmentInput
    {
        public long VersionId { get; set; }
        public long ClientId { get; set; }
        public long? ClientUnitId { get; set; }
        public string? Notes { get; set; }
    }

    public IReadOnlyList<ComponentEntity> AllComponents { get; private set; } = [];

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
            .OrderBy(v => v.ReleaseOrder)
            .ThenBy(v => v.Id)
            .ToList();

        // Fase 1, 2 e 5 (Versionamento Inteligente): itens de alteração, destinação, indicadores e casos vinculados.
        var changesMap = new System.Collections.Generic.Dictionary<long, IReadOnlyList<ProductVersionChangeDto>>();
        var assignmentsMap = new System.Collections.Generic.Dictionary<long, IReadOnlyList<ProductVersionAssignmentDto>>();
        var linkedCasesMap = new System.Collections.Generic.Dictionary<long, IReadOnlyList<VersionLinkedCaseDetailDto>>();
        var recurrenceMap = new System.Collections.Generic.Dictionary<long, FixRecurrenceObservationDto>();

        var indicatorsList = await _versionManagementService.GetVersionIndicatorsForProductAsync(productId);
        IndicatorsByVersion = indicatorsList.ToDictionary(i => i.ProductVersionId);
        CasesCountByVersion = indicatorsList.ToDictionary(i => i.ProductVersionId, i => i.CasesOccurredCount);

        foreach (var v in Versions)
        {
            changesMap[v.Id] = await _versionManagementService.GetChangesByVersionIdAsync(v.Id);
            assignmentsMap[v.Id] = await _versionManagementService.GetAssignmentsByVersionIdAsync(v.Id);
            linkedCasesMap[v.Id] = await _versionManagementService.GetVersionLinkedCasesAsync(v.Id);

            foreach (var ch in changesMap[v.Id])
            {
                if (string.Equals(ch.ChangeType, "Fix", StringComparison.OrdinalIgnoreCase))
                {
                    recurrenceMap[ch.Id] = await _versionManagementService.GetFixRecurrenceAsync(ch.Id);
                }
            }
        }
        ChangesByVersion = changesMap;
        AssignmentsByVersion = assignmentsMap;
        LinkedCasesByVersion = linkedCasesMap;
        RecurrenceByChangeId = recurrenceMap;

        // Dados para a destinação (rollout) de versões a clientes.
        ClientsForRollout = (await _clientService.GetAllClientsAsync())
            .Where(c => c.Status == "Active")
            .OrderBy(c => c.Name)
            .ToList();
        var unitsMap = new System.Collections.Generic.Dictionary<long, IReadOnlyList<ClientUnitDto>>();
        var clientContextsMap = new System.Collections.Generic.Dictionary<long, IReadOnlyList<ClientTechnicalContextDto>>();
        foreach (var c in ClientsForRollout)
        {
            var details = await _clientService.GetClientDetailsAsync(c.Id);
            unitsMap[c.Id] = details?.Units.Where(u => u.Status == "Active").OrderBy(u => u.Name).ToList() ?? [];
            clientContextsMap[c.Id] = details?.TechnicalContexts.Where(tc => tc.ProductId == productId).ToList() ?? [];
        }
        UnitsByClient = unitsMap;
        ClientContextsMap = clientContextsMap;
        AvailableEnvironments = await _catalogRepository.GetAllEnvironmentsAsync();

        Components = await _catalogService.GetAllComponentsAsync(productId);
        ComponentTypesList = await _catalogService.GetComponentTypesAsync(includeInactive: true);
        DepartmentsList = await _departmentService.GetAllDepartmentsAsync();
        AllComponents = await _catalogService.GetAllComponentsAsync(productId: null);

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
            var versionId = await _catalogService.CreateVersionAsync(
                id,
                NewVersion.VersionLabel.Trim(),
                NewVersion.ReleasedAt,
                GetCurrentUserId());

            // Seleção de clientes no rollout já na criação da versão (Item 6)
            if (SelectedClientIdsForRollout != null && SelectedClientIdsForRollout.Count > 0)
            {
                var assignedCount = 0;
                foreach (var clientId in SelectedClientIdsForRollout.Where(cid => cid > 0).Distinct())
                {
                    try
                    {
                        await _versionManagementService.CreateAssignmentAsync(
                            versionId,
                            clientId,
                            clientUnitId: null,
                            notes: "Destinação incluída no cadastro da versão.",
                            currentUserId: GetCurrentUserId());
                        assignedCount++;
                    }
                    catch
                    {
                        // Prossegue se um cliente falhar (ex: duplicidade)
                    }
                }

                SuccessMessage = assignedCount > 0
                    ? $"Versão '{NewVersion.VersionLabel}' registrada e destinada a {assignedCount} cliente(s)."
                    : $"Versão '{NewVersion.VersionLabel}' registrada para este sistema.";
            }
            else
            {
                SuccessMessage = $"Versão '{NewVersion.VersionLabel}' registrada para este sistema.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                ? $"Este sistema já tem uma versão '{NewVersion.VersionLabel}' cadastrada."
                : $"Erro ao registrar versão: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

    // --- Fase 1 & 2 (Versionamento Inteligente): alterações, vínculo de casos e destinação ----

    public async Task<PartialViewResult> OnGetPreviewChangeCaseSuggestionsAsync(
        long productVersionId,
        string? title,
        string? description,
        long? componentId,
        string? errorCode)
    {
        var version = await _catalogRepository.GetProductVersionByIdAsync(productVersionId);
        if (version == null)
        {
            return Partial("~/Pages/Shared/Partials/_ChangeCaseSuggestionList.cshtml", Array.Empty<VersionChangeCaseSuggestionDto>());
        }

        var input = new VersionChangeCaseSuggestionInput(
            ProductVersionChangeId: null,
            ProductVersionId: productVersionId,
            ProductId: version.ProductId,
            ComponentId: componentId,
            ErrorCode: errorCode,
            Title: title,
            Description: description
        );

        var results = await _versionManagementService.SuggestCasesForChangeAsync(input);
        return Partial("~/Pages/Shared/Partials/_ChangeCaseSuggestionList.cshtml", results);
    }

    public async Task<IActionResult> OnPostAddChangeAsync(long id)
    {
        if (string.IsNullOrWhiteSpace(NewChange.Title))
        {
            ErrorMessage = "O título da alteração é obrigatório.";
            return RedirectToPage(new { id, tab = "versions" });
        }

        try
        {
            var changeId = await _versionManagementService.CreateChangeAsync(
                NewChange.VersionId,
                NewChange.ChangeType,
                NewChange.Title.Trim(),
                NewChange.Description,
                NewChange.ComponentId,
                NewChange.ErrorCode,
                GetCurrentUserId());

            if (NewChange.SelectedCaseIdsToLink != null && NewChange.SelectedCaseIdsToLink.Count > 0)
            {
                var userId = GetCurrentUserId();
                foreach (var caseId in NewChange.SelectedCaseIdsToLink.Distinct())
                {
                    try
                    {
                        await _versionManagementService.LinkCaseAsync(
                            changeId,
                            caseId,
                            "FixedBy",
                            matchScore: null,
                            matchedFactorsJson: null,
                            currentUserId: userId);
                    }
                    catch
                    {
                        // Falha pontual de vínculo não impede o cadastro da alteração
                    }
                }
            }

            SuccessMessage = "Alteração registrada na versão com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar alteração: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

    public async Task<IActionResult> OnPostLinkCaseAsync(long id)
    {
        try
        {
            var caseDto = await _caseService.GetCaseByNumberAsync(NewCaseLink.CaseNumber);
            if (caseDto == null)
                throw new KeyNotFoundException($"Nenhum caso encontrado com o número #{NewCaseLink.CaseNumber}.");

            await _versionManagementService.LinkCaseAsync(
                NewCaseLink.ChangeId,
                caseDto.Id,
                NewCaseLink.RelationType,
                currentUserId: GetCurrentUserId());
            SuccessMessage = "Caso vinculado à alteração (a versão do caso não é alterada).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao vincular caso: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

    public async Task<IActionResult> OnPostUnlinkCaseAsync(long id, long changeId, long caseId, string relationType)
    {
        try
        {
            await _versionManagementService.UnlinkCaseAsync(changeId, caseId, relationType, GetCurrentUserId());
            SuccessMessage = "Caso desvinculado da alteração.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao desvincular caso: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

    public async Task<IActionResult> OnPostCreateAssignmentAsync(long id)
    {
        try
        {
            await _versionManagementService.CreateAssignmentAsync(
                NewAssignment.VersionId,
                NewAssignment.ClientId,
                NewAssignment.ClientUnitId,
                NewAssignment.Notes,
                GetCurrentUserId());
            SuccessMessage = "Destinação registrada. O deploy confirmado atualizará a versão corrente do cliente.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar destinação: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

    public async Task<IActionResult> OnPostConfirmDeployAsync(long id, long assignmentId, long? environmentId = null)
    {
        try
        {
            await _versionManagementService.ConfirmAssignmentDeployedAsync(assignmentId, environmentId, GetCurrentUserId());
            SuccessMessage = "Deploy confirmado: a versão anterior foi encerrada e a nova versão do cliente agora está ativa com histórico preservado.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao confirmar deploy: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

    public async Task<IActionResult> OnPostScheduleAssignmentAsync(long id, long assignmentId, DateTime scheduledAt)
    {
        try
        {
            await _versionManagementService.ScheduleAssignmentAsync(assignmentId, scheduledAt, GetCurrentUserId());
            SuccessMessage = "Destinação agendada com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao agendar destinação: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

    public async Task<IActionResult> OnPostFailAssignmentAsync(long id, long assignmentId, string? notes)
    {
        try
        {
            await _versionManagementService.FailAssignmentAsync(assignmentId, notes, GetCurrentUserId());
            SuccessMessage = "Destinação marcada como falha.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao marcar falha: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

    public async Task<IActionResult> OnPostRemoveAssignmentAsync(long id, long assignmentId)
    {
        try
        {
            await _versionManagementService.RemoveAssignmentAsync(assignmentId, GetCurrentUserId());
            SuccessMessage = "Destinação removida do planejamento.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao remover destinação: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

    public async Task<IActionResult> OnPostReopenAssignmentAsync(long id, long assignmentId)
    {
        try
        {
            await _versionManagementService.ReopenAssignmentAsync(assignmentId, GetCurrentUserId());
            SuccessMessage = "Destinação reaberta para o planejamento.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao reabrir destinação: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

    public async Task<IActionResult> OnPostSkipAssignmentAsync(long id, long assignmentId)
    {
        try
        {
            await _versionManagementService.SkipAssignmentAsync(assignmentId, GetCurrentUserId());
            SuccessMessage = "Destinação marcada como ignorada.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao ignorar destinação: {ex.Message}";
        }

        return RedirectToPage(new { id, tab = "versions" });
    }

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