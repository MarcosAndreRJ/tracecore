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
    long? ProductId,
    string? ContractNotes,
    string? HealthCheckUrl,
    string HealthCheckMethod,
    int HealthCheckTimeoutSeconds,
    int? HealthCheckExpectedStatusCode,
    long? CreatedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<IntegrationRunDto> Runs,
    string? Responsibility = null,
    string? HostingLocation = null,
    string? Direction = null
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
    DateTime RecordedAt,
    string TriggeredBy = "Manual",
    string? RunContext = null,
    long? CaseId = null
);

public record CreateIntegrationCommand(
    string Code,
    string Name,
    string IntegrationType,
    string? TargetSystemDescription,
    long? OwnerDepartmentId,
    string? ContractNotes,
    long? CreatedBy,
    string? HealthCheckUrl = null,
    string HealthCheckMethod = "Http",
    int HealthCheckTimeoutSeconds = 5,
    int? HealthCheckExpectedStatusCode = 200,
    long? ProductId = null,
    string? Responsibility = null,
    string? HostingLocation = null,
    string? Direction = null
);

public record UpdateIntegrationCommand(
    long Id,
    string Code,
    string Name,
    string IntegrationType,
    long? ProductId,
    string? TargetSystemDescription,
    long? OwnerDepartmentId,
    string? ContractNotes,
    string? Responsibility,
    string? HostingLocation,
    string? Direction
);

public record RegisterIntegrationRunCommand(
    long IntegrationId,
    string Status,
    DateTime? StartedAt,
    long? RecordsProcessed,
    string? ErrorMessage,
    long? RecordedBy,
    string TriggeredBy = "Manual"
);

public record ConfigureIntegrationHealthCheckCommand(
    long IntegrationId,
    string? HealthCheckUrl,
    string HealthCheckMethod,
    int HealthCheckTimeoutSeconds,
    int? HealthCheckExpectedStatusCode,
    long? UpdatedBy = null
);