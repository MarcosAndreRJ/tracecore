using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record CaseRelationDto(
    long Id,
    long SourceCaseId,
    long TargetCaseId,
    ulong TargetCaseNumber,
    string TargetTitle,
    string TargetStatus,
    string TargetSeverity,
    string? TargetProductName,
    string? TargetComponentName,
    string RelationType,
    double? SimilarityScore,
    List<string> MatchedFactors,
    long? CreatedBy,
    string? CreatedByName,
    DateTime CreatedAt
)
{
    public string TargetCaseSummary => TargetTitle;
    public string TargetCaseStatus => TargetStatus;
    public string TargetCaseSeverity => TargetSeverity;
    public DateTime TargetCaseOpenedAt => CreatedAt;
}

public record AggregatedInsightDto(
    string ActionOrComponent,
    int SampleCount,
    int SuccessCount,
    string Text
)
{
    public int SampleSize => SampleCount;
    public string Description => Text;
    public string MatchingActionOrComponent => ActionOrComponent;
}

public record CaseRelationsOverviewDto(
    long CaseId,
    List<CaseRelationDto> SimilarCases,
    List<CaseRelationDto> ManualRelations,
    AggregatedInsightDto? Insight
);

public record CreateCaseRelationCommand(
    long SourceCaseId,
    long? TargetCaseId = null,
    ulong? TargetCaseNumber = null,
    string RelationType = "Similar"
);
