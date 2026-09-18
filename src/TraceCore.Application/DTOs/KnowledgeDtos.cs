using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record CreateKnowledgeApplicabilityInput(
    long? ProductId = null,
    long? ProductVersionId = null,
    long? ComponentId = null,
    long? EnvironmentId = null,
    string ApplicabilityType = "Applies",
    string? Notes = null
);

public record CreateKnowledgeStepInput(
    int SequenceNo,
    string Title,
    string Description,
    string StepType = "Solution",
    string? Command = null,
    string? ExpectedOutput = null
);

public record CreateKnowledgeDraftCommand(
    string Title,
    string Summary,
    string KnowledgeType = "Solution",
    string ProvenanceType = "Case",
    long? ProvenanceCaseId = null,
    string? ProvenanceReference = null,
    long? OwnerDepartmentId = null,
    string ContentMarkdown = "",
    string? ProblemDescription = null,
    string? RootCauseSummary = null,
    string? ValidationMethod = null,
    string? RiskWarning = null,
    string? RollbackPlan = null,
    List<CreateKnowledgeApplicabilityInput>? Applicabilities = null,
    List<CreateKnowledgeStepInput>? Steps = null,
    List<string>? Symptoms = null,
    List<string>? Technologies = null,
    List<string>? Tags = null
);

public record CreateDraftFromCaseCommand(
    long CaseId
);

public record SubmitForReviewCommand(
    long KnowledgeItemId
);

public record ApproveAndPublishCommand(
    long KnowledgeItemId,
    DateTime? NextReviewDue = null
);

public record CreateNewVersionCommand(
    long KnowledgeItemId,
    string ContentMarkdown,
    string? ProblemDescription = null,
    string? RootCauseSummary = null,
    string? ValidationMethod = null,
    string? RiskWarning = null,
    string? RollbackPlan = null,
    string? ChangeSummary = null,
    List<CreateKnowledgeStepInput>? Steps = null,
    List<string>? Symptoms = null
);

public record DeprecateKnowledgeCommand(
    long KnowledgeItemId,
    long? ReplacementKnowledgeId = null
);

public record ArchiveKnowledgeCommand(
    long KnowledgeItemId,
    string? Reason = null
);

public record RecordKnowledgeUsageCommand(
    long KnowledgeItemId,
    long CaseId,
    string Outcome,
    string? Notes = null,
    string? ContextMatchJson = null
);

public record KnowledgeItemSummaryDto(
    long Id,
    string Code,
    string Title,
    string Summary,
    string ProductCode,
    string ProductName,
    string Component,
    string ApplicableVersions,
    string Status,
    string ProvenanceType,
    string? ProvenanceRef,
    string Author,
    string Reviewer,
    string SuccessRate,
    string SuccessSample,
    int TotalUsages,
    int SuccessfulUsages,
    string RiskLevel,
    bool HasRollbackPlan,
    DateTime UpdatedAt,
    string Category,
    List<string> Tags
);

public record KnowledgeStepDto(
    int StepNumber,
    string Title,
    string Description,
    string? Command,
    string? ExpectedOutput,
    string StepType = "Solution"
);

public record AssociatedCaseRefDto(
    long CaseId,
    string Code,
    string Title,
    DateTime ResolutionDate,
    string Outcome
);

public record RevisionHistoryDto(
    string Version,
    string ChangedBy,
    DateTime ChangedAt,
    string Reason
);

public record KnowledgeDetailDto(
    long Id,
    string Code,
    string Title,
    string Version,
    string LifecycleStatus,
    bool IsAiGenerated,
    string Author,
    string Reviewer,
    DateTime? PublishedAt,
    DateTime? LastReviewedAt,
    DateTime? NextReviewDue,
    string ProvenanceType,
    long? SourceCaseId,
    string SourceCaseTitle,
    string SourceReference,
    string ProductCode,
    string ProductName,
    string Component,
    string ApplicableVersions,
    List<string> ApplicableEnvironments,
    List<string> Prerequisites,
    string ProblemDescription,
    string? RootCauseSummary,
    string RiskLevel,
    string RiskWarning,
    string RollbackPlan,
    List<KnowledgeStepDto> Steps,
    string ValidationMethod,
    string? ValidationCommand,
    string? ExpectedValidationOutput,
    int TotalUsages,
    int SuccessfulUsages,
    string SuccessRate,
    string SuccessSample,
    List<AssociatedCaseRefDto> AssociatedCases,
    List<RevisionHistoryDto> Revisions,
    List<string> Technologies,
    List<string> Tags
);
