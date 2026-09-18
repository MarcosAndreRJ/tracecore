using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record IntegrationDto(
    long Id,
    string Code,
    string Name,
    string IntegrationType,
    string? TargetSystemDescription,
    string Status,
    long? OwnerDepartmentId,
    string? OwnerDepartmentName,
    string? ContractNotes,
    long? CreatedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<IntegrationRunDto> Runs
);

public record IntegrationRunDto(
    long Id,
    long IntegrationId,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string Status,
    long? RecordsProcessed,
    string? ErrorMessage,
    long? RecordedBy,
    DateTime RecordedAt
);

public record CreateIntegrationCommand(
    string Code,
    string Name,
    string IntegrationType,
    string? TargetSystemDescription,
    long? OwnerDepartmentId,
    string? ContractNotes,
    long? CreatedBy
);

public record RegisterIntegrationRunCommand(
    long IntegrationId,
    string Status,
    DateTime? StartedAt,
    long? RecordsProcessed,
    string? ErrorMessage,
    long? RecordedBy
);