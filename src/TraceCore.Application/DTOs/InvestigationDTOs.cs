using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record RegisterHypothesisCommand(
    long CaseId,
    string Title,
    string? Description = null,
    long? ComponentId = null
);

public record RegisterHypothesisRequest(
    string Title,
    string? Description = null,
    long? ComponentId = null
);

public record RegisterDiagnosticStepCommand(
    long CaseId,
    long? HypothesisId,
    string Title,
    string? Objective,
    string? Instruction,
    string? InputEvidenceSummary,
    string ResultSummary,
    string Outcome,
    string? StepType = "Verification",
    string? RiskLevel = "Low",
    int? DurationSeconds = null
);

public record RegisterDiagnosticStepRequest(
    long? HypothesisId,
    string Title,
    string? Objective,
    string? Instruction,
    string? InputEvidenceSummary,
    string ResultSummary,
    string Outcome,
    string? StepType = "Verification",
    string? RiskLevel = "Low",
    int? DurationSeconds = null
);

public record EvaluateHypothesisCommand(
    long HypothesisId,
    string NewStatus,
    string Justification,
    long? EvidenceStepId = null
);

public record EvaluateHypothesisRequest(
    string NewStatus,
    string Justification,
    long? EvidenceStepId = null
);

public record CaseHypothesisDto(
    long Id,
    long CaseId,
    long? ComponentId,
    string? ComponentName,
    string Title,
    string? Description,
    string Status,
    string SourceType,
    string? Justification,
    DateTime CreatedAt,
    long? CreatedBy,
    string? CreatedByName,
    DateTime UpdatedAt,
    List<DiagnosticStepDto> Steps
);

public record EvidenceSuggestionDto(
    long CaseId,
    long? HypothesisId,
    string? HypothesisTitle,
    long DiagnosticStepId,
    string SuggestedEvidenceType,
    string SuggestedDescription,
    string SuggestedRelationType,
    string SuggestedJustification
);

public record DiagnosticStepDto(
    long Id,
    long DiagnosticSessionId,
    int SequenceNo,
    string StepType,
    long? HypothesisId,
    string? HypothesisTitle,
    string Title,
    string? Objective,
    string? Instruction,
    string? InputEvidenceSummary,
    string ResultSummary,
    string Outcome,
    string RiskLevel,
    int? DurationSeconds,
    long? PerformedBy,
    string? PerformedByName,
    DateTime PerformedAt,
    string? MetadataJson,
    EvidenceSuggestionDto? SuggestedEvidence = null
);

public record RecordEvidenceCommand(
    long CaseId,
    string EvidenceType,
    string Description,
    long? AttachmentId = null,
    long? DiagnosticStepId = null,
    long? CaseIterationId = null,
    List<HypothesisEvidenceRelationInputDto>? HypothesisRelations = null
);

public record HypothesisEvidenceRelationInputDto(
    long HypothesisId,
    string RelationType,
    string? Justification = null
);

public record DiagnosticSessionDto(
    long Id,
    long CaseId,
    string Status,
    DateTime StartedAt,
    long StartedBy,
    string? StartedByName,
    DateTime? EndedAt
);

public record InvestigationTimelineItemDto(
    long Id,
    string ItemType, // "Hypothesis" | "Step"
    DateTime TimestampUtc,
    string Title,
    string? Subtitle,
    string StatusOrOutcome,
    long? HypothesisId,
    string? HypothesisTitle,
    string? PerformedByName,
    string? Details
);

public record CaseInvestigationTimelineDto(
    long CaseId,
    ulong CaseNumber,
    DiagnosticSessionDto? ActiveSession,
    IReadOnlyList<CaseHypothesisDto> Hypotheses,
    IReadOnlyList<DiagnosticStepDto> Steps,
    IReadOnlyList<InvestigationTimelineItemDto> Timeline
);
