using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record ProductTechnicalProfileDto(
    long Id,
    long ProductId,
    string? BusinessPurpose,
    string? ArchitectureSummary,
    string? FrontendStack,
    string? BackendStack,
    string? PrimaryDatabase,
    string? RuntimePlatform,
    string? HostingModel,
    string? AuthenticationModel,
    string? ObservabilityStack,
    string? DeploymentModel,
    string? Vendor,
    string? SupportNotes,
    string? KnownConstraints,
    string? InvestigationNotes,
    string ExternalResearchPolicy,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? SystemType = null
);

public record UpsertProductTechnicalProfileCommand(
    long ProductId,
    string? BusinessPurpose,
    string? ArchitectureSummary,
    string? FrontendStack,
    string? BackendStack,
    string? PrimaryDatabase,
    string? RuntimePlatform,
    string? HostingModel,
    string? AuthenticationModel,
    string? ObservabilityStack,
    string? DeploymentModel,
    string? Vendor,
    string? SupportNotes,
    string? KnownConstraints,
    string? InvestigationNotes,
    string ExternalResearchPolicy,
    string? SystemType = null
);

public record ProductTechnicalSourceDto(
    long Id,
    long ProductId,
    string Name,
    string SourceType,
    string? Url,
    string? Description,
    string TrustLevel,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateProductTechnicalSourceCommand(
    long ProductId,
    string Name,
    string SourceType,
    string? Url,
    string? Description,
    string TrustLevel
);

public record UpdateProductTechnicalSourceCommand(
    long SourceId,
    string Name,
    string SourceType,
    string? Url,
    string? Description,
    string TrustLevel
);

public record ProductExternalResearchDomainDto(
    long Id,
    long ProductId,
    string Domain,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);

public record ProductVersionSummaryDto(
    long Id,
    string VersionLabel,
    string Status
);

public record ComponentSummaryDto(
    long Id,
    string Name,
    string ComponentType,
    string Status
);

public record DependencySummaryDto(
    long Id,
    string SourceComponentName,
    string TargetComponentName,
    string DependencyType,
    string Criticality
);

public record IntegrationSummaryDto(
    long Id,
    string Code,
    string Name,
    string IntegrationType,
    string Status,
    string? Responsibility = null,
    string? HostingLocation = null,
    string? Direction = null
);

/// <summary>
/// Contexto consolidado de investigação de um produto (Prompt 2, §24/§25/§26).
/// Montado no backend com poucas queries — evita que o consumidor (futuro Copiloto,
/// Prompt 3) precise conhecer as tabelas internas ou fazer N+1 chamadas.
/// Dados estruturados, nunca texto pronto — quem consome decide como usar.
/// </summary>
public record ProductInvestigationContextDto(
    long ProductId,
    string ProductName,
    string? ProductCode,
    string? ProductDescription,
    bool IsExternal,
    ProductVersionSummaryDto? SelectedVersion,
    ProductTechnicalProfileDto? TechnicalProfile,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<ComponentSummaryDto> Components,
    IReadOnlyList<DependencySummaryDto> Dependencies,
    IReadOnlyList<IntegrationSummaryDto> Integrations,
    IReadOnlyList<ProductTechnicalSourceDto> TechnicalSources,
    string ExternalResearchPolicy,
    IReadOnlyList<string> AllowedDomains
);
