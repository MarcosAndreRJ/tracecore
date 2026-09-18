using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record SearchFilterCriteria(
    long? ProductId = null,
    string? ProductName = null,
    long? ProductVersionId = null,
    string? VersionName = null,
    long? ClientId = null,
    string? ClientName = null,
    long? ClientUnitId = null,
    string? ClientUnitName = null,
    long? DepartmentId = null,
    string? DepartmentName = null,
    long? TechnologyId = null,
    string? TechnologyName = null,
    long? ComponentId = null,
    string? ComponentName = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? ErrorCode = null,
    long? RootCauseId = null,
    string? RootCauseName = null,
    string? Status = null,
    string? Environment = null
);

public record SearchFilterChipDto(
    string FilterKey,
    string Label,
    string Value
);

public record ExecuteSearchCommand(
    string? QueryText,
    SearchFilterCriteria? Filters = null,
    long? ContextCaseId = null,
    string? SelectedType = null, // "All", "Case", "Solution", "System", "Component", "Error", "RootCause"
    int PageNumber = 1,
    int PageSize = 25
);

public record SearchResultItemDto(
    string Type, // "Case", "Solution", "System", "Component", "Error", "RootCause"
    long Id,
    string Code,
    string Title,
    string Subtitle,
    string Snippet,
    string? Status,
    double Score,
    List<string> MatchedFactors,
    string? SuccessRate,
    string? SuccessSample,
    string Url,
    int Position
);

public record SearchResultsResponseDto(
    long QueryId,
    string QueryText,
    int DurationMs,
    int TotalCount,
    List<SearchResultItemDto> Items,
    Dictionary<string, int> CountByType,
    List<SearchFilterChipDto> ActiveFilterChips,
    List<SearchResultItemDto>? ExactMatches = null
)
{
    public List<SearchResultItemDto> ExactMatches { get; init; } = ExactMatches ?? new();
}

public record RecordResultInteractionCommand(
    long QueryId,
    string ResultType,
    long ResultId,
    int Position
);

public record RecordFeedbackCommand(
    long InteractionId,
    bool Useful
);
