using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record SearchableContentFilterDto(
    string? SourceType = null,
    string? ValidationStatus = null,
    string? QualityStatus = null,
    string? Visibility = null,
    long? ClientId = null,
    long? ProductId = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50
);

public record SearchableContentEntryDto(
    long Id,
    string SourceType,
    long SourceId,
    long? SourceVersionId,
    string Title,
    string NormalizedContent,
    string ContentHash,
    string ValidationStatus,
    string QualityStatus,
    string Visibility,
    string ReadinessStatus, // Ready, NeedsMetadata, NeedsReview, NotEligible
    string ReadinessReason,
    long? ClientId,
    long? ProductId,
    string? ComponentIdsJson,
    string? MetadataJson,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime SourceUpdatedAt,
    string? SourceUrl
);

public record ContentQualityMetricsDto(
    int TotalCount,
    int ReadyCount,
    int NeedsMetadataCount,
    int NeedsReviewCount,
    int NotEligibleCount,
    IReadOnlyDictionary<string, int> BySourceType,
    IReadOnlyDictionary<string, int> ByQualityStatus
);

public record ContentSearchResultDto(
    IReadOnlyList<SearchableContentEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
