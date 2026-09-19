using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record DiagnosticFlowDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    string EntryKeywords,
    string Status,
    int HypothesesCount,
    int ChecksCount,
    DateTime CreatedAt
);

public record DiagnosticFlowHypothesisDto(
    long Id,
    long FlowId,
    string Title,
    string? Description,
    long? AssociatedComponentId,
    string? AssociatedComponentName = null
);

public record DiagnosticCheckImpactDto(
    long Id,
    long CheckOptionId,
    long FlowHypothesisId,
    string ImpactType,
    decimal Weight,
    string? FlowHypothesisTitle = null
);

public record DiagnosticCheckOptionDto(
    long Id,
    long CheckId,
    string OptionText,
    int OrderNo,
    List<DiagnosticCheckImpactDto> Impacts
);

public record DiagnosticCheckDto(
    long Id,
    long FlowId,
    string Code,
    string Title,
    string QuestionText,
    string CheckType,
    int Cost,
    string RiskLevel,
    string? SkipConditionField,
    List<DiagnosticCheckOptionDto> Options,
    long? IntegrationId = null
);

public record DiagnosticFlowDetailsDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    string EntryKeywords,
    string Status,
    List<DiagnosticFlowHypothesisDto> Hypotheses,
    List<DiagnosticCheckDto> Checks
);

public record DiagnosticRecommendationDto(
    long CheckId,
    string CheckCode,
    string Title,
    string QuestionText,
    int Cost,
    string RiskLevel,
    double HeuristicScore,
    string Explanation,
    List<DiagnosticCheckOptionDto> Options,
    bool SuggestEscalation = false
);

public record InvestigativeRankedHypothesisDto(
    long Id,
    string Title,
    string Status,
    int PriorityRank,
    string PriorityLabel,
    string? AssociatedComponent,
    decimal FavorsWeight,
    decimal DiscardsWeight,
    string? Justification
);

public record DiagnosticEngineStateDto(
    long CaseId,
    DiagnosticFlowDto? ActiveFlow,
    List<InvestigativeRankedHypothesisDto> Hypotheses,
    DiagnosticRecommendationDto? CurrentRecommendation,
    int AnsweredChecksCount,
    bool SuggestEscalation,
    List<DiagnosticFlowDto> AvailableFlows
);

public record CreateDiagnosticFlowCommand(
    string Code,
    string Name,
    string EntryKeywords,
    string? Description = null,
    string Status = "Active"
);

public record CreateDiagnosticCheckCommand(
    long FlowId,
    string Code,
    string Title,
    string QuestionText,
    string CheckType = "Question",
    int Cost = 1,
    string RiskLevel = "Low",
    string? SkipConditionField = null,
    long? IntegrationId = null
);
