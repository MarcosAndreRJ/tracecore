using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record ClientDto(
    long Id,
    string? Code,
    string Name,
    string Status,
    string? ExternalCrmId = null,
    string? Notes = null,
    DateTime CreatedAt = default,
    int UnitsCount = 0,
    int TechnicalContextsCount = 0
);

public record ClientDetailsDto(
    long Id,
    string? Code,
    string Name,
    string Status,
    string? ExternalCrmId,
    string? Notes,
    DateTime CreatedAt,
    IReadOnlyList<ClientUnitDto> Units,
    IReadOnlyList<ClientTechnicalContextDto> TechnicalContexts
);

public record ClientUnitDto(
    long Id,
    long ClientId,
    string Code,
    string Name,
    string Status,
    string? ExternalCrmId,
    DateTime CreatedAt
);

public record ClientTechnicalContextDto(
    long Id,
    long ClientId,
    long? ClientUnitId,
    string? UnitName,
    long ProductId,
    string ProductName,
    long? ProductVersionId,
    string? VersionLabel,
    long? EnvironmentId,
    string? EnvironmentName,
    string Status,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    DateTime CreatedAt
);

public record CreateClientRequest(
    string Name,
    string? Code = null,
    string? ExternalCrmId = null,
    string? Notes = null
);

public record UpdateClientRequest(
    string Name,
    string? Code = null,
    string Status = "Active",
    string? ExternalCrmId = null,
    string? Notes = null
);

public record CreateClientUnitRequest(
    string Code,
    string Name,
    string? ExternalCrmId = null
);

public record CreateTechnicalContextRequest(
    long ProductId,
    long? ClientUnitId = null,
    long? ProductVersionId = null,
    long? EnvironmentId = null,
    string Status = "Active",
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null
);
