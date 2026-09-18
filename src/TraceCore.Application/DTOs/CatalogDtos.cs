using System;

namespace TraceCore.Application.DTOs;


public record ProductDto(
    long Id,
    string? Code,
    string Name,
    string? Description,
    string Status
);

public record ProductVersionDto(
    long Id,
    long ProductId,
    string VersionLabel,
    string Status
);

public record EnvironmentDto(
    long Id,
    string Name,
    string EnvironmentType
);

public record ComponentDto(
    long Id,
    long? ProductId,
    string? Code,
    string Name,
    string ComponentType,
    string? Description,
    long? OwnerDepartmentId,
    string Status
);
