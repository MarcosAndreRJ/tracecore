using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class ProductTechnicalContextService : IProductTechnicalContextService
{
    private readonly IProductTechnicalContextRepository _repository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IIntegrationRepository _integrationRepository;
    private readonly IAuditService _auditService;

    public ProductTechnicalContextService(
        IProductTechnicalContextRepository repository,
        ICatalogRepository catalogRepository,
        IIntegrationRepository integrationRepository,
        IAuditService auditService)
    {
        _repository = repository;
        _catalogRepository = catalogRepository;
        _integrationRepository = integrationRepository;
        _auditService = auditService;
    }

    public async Task<ProductTechnicalProfileDto?> GetTechnicalProfileAsync(long productId, CancellationToken ct = default)
    {
        var profile = await _repository.GetProfileByProductIdAsync(productId, ct);
        return profile == null ? null : ToDto(profile);
    }

    public async Task UpsertTechnicalProfileAsync(UpsertProductTechnicalProfileCommand command, long? currentUserId, CancellationToken ct = default)
    {
        if (command == null)
            throw new ArgumentException("Dados do perfil técnico são obrigatórios.", nameof(command));
        if (command.ProductId <= 0)
            throw new ArgumentException("ProductId inválido.", nameof(command.ProductId));

        var product = await _catalogRepository.GetProductByIdAsync(command.ProductId, ct)
            ?? throw new KeyNotFoundException($"Produto com ID {command.ProductId} não encontrado.");

        var policy = string.IsNullOrWhiteSpace(command.ExternalResearchPolicy) ? "Disabled" : command.ExternalResearchPolicy.Trim();
        if (!ProductTechnicalProfile.ValidExternalResearchPolicies.Contains(policy, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Política de pesquisa externa inválida: '{policy}'. Valores aceitos: {string.Join(", ", ProductTechnicalProfile.ValidExternalResearchPolicies)}.",
                nameof(command.ExternalResearchPolicy));
        }

        var existing = await _repository.GetProfileByProductIdAsync(command.ProductId, ct);

        var profile = new ProductTechnicalProfile
        {
            ProductId = command.ProductId,
            BusinessPurpose = Trim(command.BusinessPurpose),
            ArchitectureSummary = Trim(command.ArchitectureSummary),
            FrontendStack = Trim(command.FrontendStack),
            BackendStack = Trim(command.BackendStack),
            PrimaryDatabase = Trim(command.PrimaryDatabase),
            RuntimePlatform = Trim(command.RuntimePlatform),
            HostingModel = Trim(command.HostingModel),
            AuthenticationModel = Trim(command.AuthenticationModel),
            ObservabilityStack = Trim(command.ObservabilityStack),
            DeploymentModel = Trim(command.DeploymentModel),
            Vendor = Trim(command.Vendor),
            SupportNotes = Trim(command.SupportNotes),
            KnownConstraints = Trim(command.KnownConstraints),
            InvestigationNotes = Trim(command.InvestigationNotes),
            ExternalResearchPolicy = policy,
            CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow,
            CreatedBy = existing?.CreatedBy ?? currentUserId,
            UpdatedAt = existing == null ? null : DateTime.UtcNow,
            UpdatedBy = existing == null ? null : currentUserId
        };

        await _repository.UpsertProfileAsync(profile, ct);

        await _auditService.RecordAsync(
            action: "product_technical_profile.upsert",
            entityType: "product_technical_profiles",
            entityId: command.ProductId.ToString(),
            actorUserId: currentUserId,
            after: new { profile.ProductId, profile.ExternalResearchPolicy },
            ct: ct);
    }

    public Task<IReadOnlyList<string>> GetProductTechnologiesAsync(long productId, CancellationToken ct = default)
        => _repository.GetTechnologyNamesByProductIdAsync(productId, ct);

    public async Task SetProductTechnologiesAsync(long productId, IReadOnlyList<string> technologyNames, long? currentUserId, CancellationToken ct = default)
    {
        _ = await _catalogRepository.GetProductByIdAsync(productId, ct)
            ?? throw new KeyNotFoundException($"Produto com ID {productId} não encontrado.");

        await _repository.SetProductTechnologiesAsync(productId, technologyNames ?? Array.Empty<string>(), ct);

        await _auditService.RecordAsync(
            action: "product_technology.set",
            entityType: "product_technologies",
            entityId: productId.ToString(),
            actorUserId: currentUserId,
            after: new { ProductId = productId, Technologies = technologyNames },
            ct: ct);
    }

    public async Task<IReadOnlyList<ProductTechnicalSourceDto>> GetTechnicalSourcesAsync(long productId, CancellationToken ct = default)
    {
        var sources = await _repository.GetSourcesByProductIdAsync(productId, includeInactive: true, ct);
        return sources.Select(ToDto).ToList();
    }

    public async Task<long> AddTechnicalSourceAsync(CreateProductTechnicalSourceCommand command, long? currentUserId, CancellationToken ct = default)
    {
        if (command == null)
            throw new ArgumentException("Dados da fonte técnica são obrigatórios.", nameof(command));

        _ = await _catalogRepository.GetProductByIdAsync(command.ProductId, ct)
            ?? throw new KeyNotFoundException($"Produto com ID {command.ProductId} não encontrado.");

        var source = new ProductTechnicalSource(
            command.ProductId,
            command.Name,
            command.SourceType,
            command.Url,
            command.Description,
            command.TrustLevel)
        {
            CreatedBy = currentUserId
        };

        ValidateSourceUrl(source.Url);

        var id = await _repository.AddSourceAsync(source, ct);

        await _auditService.RecordAsync(
            action: "product_technical_source.create",
            entityType: "product_technical_sources",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { source.ProductId, source.Name, source.SourceType, source.TrustLevel },
            ct: ct);

        return id;
    }

    public async Task UpdateTechnicalSourceAsync(UpdateProductTechnicalSourceCommand command, long? currentUserId, CancellationToken ct = default)
    {
        if (command == null)
            throw new ArgumentException("Dados da fonte técnica são obrigatórios.", nameof(command));

        var source = await _repository.GetSourceByIdAsync(command.SourceId, ct)
            ?? throw new KeyNotFoundException($"Fonte técnica com ID {command.SourceId} não encontrada.");

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Nome da fonte técnica é obrigatório.", nameof(command.Name));
        if (string.IsNullOrWhiteSpace(command.SourceType))
            throw new ArgumentException("Tipo da fonte técnica é obrigatório.", nameof(command.SourceType));

        ValidateSourceUrl(command.Url);

        var before = new { source.Name, source.SourceType, source.Url, source.TrustLevel };

        source.Name = command.Name.Trim();
        source.SourceType = command.SourceType.Trim();
        source.Url = string.IsNullOrWhiteSpace(command.Url) ? null : command.Url.Trim();
        source.Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        source.TrustLevel = string.IsNullOrWhiteSpace(command.TrustLevel) ? "Reference" : command.TrustLevel.Trim();
        source.UpdatedBy = currentUserId;

        await _repository.UpdateSourceAsync(source, ct);

        await _auditService.RecordAsync(
            action: "product_technical_source.update",
            entityType: "product_technical_sources",
            entityId: source.Id.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { source.Name, source.SourceType, source.Url, source.TrustLevel },
            ct: ct);
    }

    public async Task DisableTechnicalSourceAsync(long sourceId, long? currentUserId, CancellationToken ct = default)
    {
        _ = await _repository.GetSourceByIdAsync(sourceId, ct)
            ?? throw new KeyNotFoundException($"Fonte técnica com ID {sourceId} não encontrada.");

        await _repository.SetSourceActiveAsync(sourceId, false, currentUserId, ct);

        await _auditService.RecordAsync(
            action: "product_technical_source.disable",
            entityType: "product_technical_sources",
            entityId: sourceId.ToString(),
            actorUserId: currentUserId,
            ct: ct);
    }

    public async Task<IReadOnlyList<ProductExternalResearchDomainDto>> GetAllowedDomainsAsync(long productId, CancellationToken ct = default)
    {
        var domains = await _repository.GetAllowedDomainsByProductIdAsync(productId, includeInactive: true, ct);
        return domains.Select(ToDto).ToList();
    }

    public async Task<long> AddAllowedDomainAsync(long productId, string domain, string? description, long? currentUserId, CancellationToken ct = default)
    {
        _ = await _catalogRepository.GetProductByIdAsync(productId, ct)
            ?? throw new KeyNotFoundException($"Produto com ID {productId} não encontrado.");

        var normalized = NormalizeDomain(domain);

        var entity = new ProductExternalResearchDomain(productId, normalized, description)
        {
            CreatedBy = currentUserId
        };

        var id = await _repository.AddAllowedDomainAsync(entity, ct);

        await _auditService.RecordAsync(
            action: "product_external_research_domain.create",
            entityType: "product_external_research_domains",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { ProductId = productId, Domain = normalized },
            ct: ct);

        return id;
    }

    public async Task RemoveAllowedDomainAsync(long domainId, long? currentUserId, CancellationToken ct = default)
    {
        var removed = await _repository.RemoveAllowedDomainAsync(domainId, ct);
        if (!removed)
            throw new KeyNotFoundException($"Domínio permitido com ID {domainId} não encontrado.");

        await _auditService.RecordAsync(
            action: "product_external_research_domain.remove",
            entityType: "product_external_research_domains",
            entityId: domainId.ToString(),
            actorUserId: currentUserId,
            ct: ct);
    }

    public async Task<ProductInvestigationContextDto?> GetInvestigationContextAsync(long productId, long? productVersionId, CancellationToken ct = default)
    {
        var product = await _catalogRepository.GetProductByIdAsync(productId, ct);
        if (product == null) return null;

        ProductVersionSummaryDto? selectedVersion = null;
        if (productVersionId.HasValue)
        {
            var version = await _catalogRepository.GetProductVersionByIdAsync(productVersionId.Value, ct);
            if (version != null && version.ProductId == productId)
            {
                selectedVersion = new ProductVersionSummaryDto(version.Id, version.VersionLabel, version.Status);
            }
        }

        var profileEntity = await _repository.GetProfileByProductIdAsync(productId, ct);
        var technologies = await _repository.GetTechnologyNamesByProductIdAsync(productId, ct);

        var components = await _catalogRepository.GetAllComponentsAsync(productId, ct);
        var componentIds = components.Select(c => c.Id).ToHashSet();

        var allDependencies = await _catalogRepository.GetComponentDependenciesAsync(ct: ct);
        var dependencies = allDependencies
            .Where(d => componentIds.Contains(d.SourceComponentId) || componentIds.Contains(d.TargetComponentId))
            .Select(d => new DependencySummaryDto(
                d.Id,
                d.SourceComponentName ?? $"Componente #{d.SourceComponentId}",
                d.TargetComponentName ?? $"Componente #{d.TargetComponentId}",
                d.DependencyType,
                d.Criticality))
            .ToList();

        var integrations = await _integrationRepository.GetIntegrationsByProductIdAsync(productId, ct);
        var sources = await _repository.GetSourcesByProductIdAsync(productId, includeInactive: false, ct);
        var allowedDomains = await _repository.GetAllowedDomainsByProductIdAsync(productId, includeInactive: false, ct);

        return new ProductInvestigationContextDto(
            ProductId: product.Id,
            ProductName: product.Name,
            ProductCode: product.Code,
            ProductDescription: product.Description,
            IsExternal: product.IsExternal,
            SelectedVersion: selectedVersion,
            TechnicalProfile: profileEntity == null ? null : ToDto(profileEntity),
            Technologies: technologies,
            Components: components.Select(c => new ComponentSummaryDto(c.Id, c.Name, c.ComponentType, c.Status)).ToList(),
            Dependencies: dependencies,
            Integrations: integrations.Select(i => new IntegrationSummaryDto(i.Id, i.Code, i.Name, i.IntegrationType, i.Status)).ToList(),
            TechnicalSources: sources.Select(ToDto).ToList(),
            ExternalResearchPolicy: profileEntity?.ExternalResearchPolicy ?? "Disabled",
            AllowedDomains: allowedDomains.Select(d => d.Domain).ToList()
        );
    }

    private static void ValidateSourceUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            throw new ArgumentException("A URL da fonte técnica deve usar http:// ou https://.", nameof(url));
        }
    }

    private static string NormalizeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domínio é obrigatório.", nameof(domain));

        var trimmed = domain.Trim().ToLowerInvariant();

        // Remove esquema/caminho se o usuário colar uma URL inteira — só o hostname é armazenado.
        if (trimmed.Contains("://"))
        {
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                trimmed = uri.Host;
            }
            else
            {
                throw new ArgumentException($"Domínio inválido: '{domain}'. Informe apenas o domínio (ex.: dev.mysql.com), sem esquema não-web.", nameof(domain));
            }
        }

        trimmed = trimmed.TrimEnd('/');

        if (trimmed.Contains('/') || trimmed.Contains(' ') || trimmed.Contains(':'))
            throw new ArgumentException($"Domínio inválido: '{domain}'.", nameof(domain));

        if (!Uri.CheckHostName(trimmed).Equals(UriHostNameType.Dns))
            throw new ArgumentException($"Domínio inválido: '{domain}'.", nameof(domain));

        return trimmed;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProductTechnicalProfileDto ToDto(ProductTechnicalProfile p) => new(
        p.Id, p.ProductId, p.BusinessPurpose, p.ArchitectureSummary, p.FrontendStack, p.BackendStack,
        p.PrimaryDatabase, p.RuntimePlatform, p.HostingModel, p.AuthenticationModel, p.ObservabilityStack,
        p.DeploymentModel, p.Vendor, p.SupportNotes, p.KnownConstraints, p.InvestigationNotes,
        p.ExternalResearchPolicy, p.CreatedAt, p.UpdatedAt);

    private static ProductTechnicalSourceDto ToDto(ProductTechnicalSource s) => new(
        s.Id, s.ProductId, s.Name, s.SourceType, s.Url, s.Description, s.TrustLevel, s.IsActive, s.CreatedAt, s.UpdatedAt);

    private static ProductExternalResearchDomainDto ToDto(ProductExternalResearchDomain d) => new(
        d.Id, d.ProductId, d.Domain, d.Description, d.IsActive, d.CreatedAt);
}
