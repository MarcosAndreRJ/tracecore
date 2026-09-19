using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record AuditFilterDto(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    long? ActorUserId = null,
    string? Action = null,
    string? EntityType = null,
    string? EntityId = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50
);

public record AuditEntryDto(
    long Id,
    DateTime OccurredAt,
    long? ActorUserId,
    string? ActorUserName,
    string ActorType,
    string Action,
    string EntityType,
    string EntityId,
    string? CorrelationId,
    string? IpAddress,
    string? UserAgentSummary,
    string? BeforeJson,
    string? AfterJson,
    string? MetadataJson,
    string? EntityUrl
);

public record AuditSearchResultDto(
    IReadOnlyList<AuditEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
