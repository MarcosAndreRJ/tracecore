using System;
using System.Collections.Generic;
using System.IO;

namespace TraceCore.Application.DTOs;

public record OpenCaseCommand(
    string OriginalReport,
    string? Severity = "Medium",
    string? ImpactLevel = null,
    long? ClientId = null,
    long? ProductId = null,
    long? ProductVersionId = null,
    long? EnvironmentId = null,
    List<long>? ComponentIds = null,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? ScopeType = null,
    string? ExternalReference = null,
    long? CurrentOwnerUserId = null,
    long? CurrentDepartmentId = null,
    DateTime? OpenedAt = null,
    List<string>? Symptoms = null,
    List<CaseEvidenceInputDto>? Evidences = null,
    List<AttachmentInputDto>? Attachments = null
);

public record CaseEvidenceInputDto(
    string EvidenceType,
    string Description
);

public record AttachmentInputDto(
    string FileName,
    string MimeType,
    Stream ContentStream,
    string Confidentiality = "Internal"
);

public record CaseDto(
    long Id,
    ulong CaseNumber,
    string? ExternalReference,
    string SourceType,
    long? ClientId,
    string? ClientName,
    long? ProductId,
    string? ProductName,
    long? ProductVersionId,
    string? VersionLabel,
    long? EnvironmentId,
    string? EnvironmentName,
    string OriginalReport,
    string? NormalizedSummary,
    string? ExpectedBehavior,
    string? ObservedBehavior,
    string? ErrorCode,
    string? ErrorMessage,
    string? ScopeType,
    string Severity,
    string? ImpactLevel,
    string Status,
    long? CurrentOwnerUserId,
    string? CurrentOwnerUserName,
    long? CurrentDepartmentId,
    string? CurrentDepartmentName,
    string RootCauseStatus,
    DateTime OpenedAt,
    DateTime CreatedAt,
    long? CreatedBy,
    DateTime UpdatedAt,
    long? UpdatedBy,
    long RowVersion,
    IReadOnlyList<CaseSymptomDto> Symptoms,
    IReadOnlyList<CaseComponentDto> AffectedComponents,
    IReadOnlyList<CaseEvidenceDto> Evidences,
    IReadOnlyList<AttachmentDto> Attachments,
    CaseResolutionDto? Resolution = null,
    IReadOnlyList<CaseIterationDto>? Iterations = null
);

public record CaseIterationDto(
    long Id,
    long CaseId,
    int SequenceNumber,
    DateTime OpenedAt,
    long OpenedBy,
    string? OpenedByName,
    string? Reason,
    DateTime? ClosedAt,
    string Status
);

public record CaseSymptomDto(
    long Id,
    long CaseId,
    string? SymptomCode,
    string SymptomText,
    string Source,
    bool Confirmed
);

public record CaseComponentDto(
    long CaseId,
    long ComponentId,
    string? ComponentName,
    string? ComponentCode,
    string RelationType,
    string? ConfidenceLabel
);

public record CaseEvidenceDto(
    long Id,
    long CaseId,
    string EvidenceType,
    string Description,
    long? AttachmentId,
    string? AttachmentFileName,
    long? CreatedBy,
    DateTime CreatedAt,
    long CaseIterationId = 0,
    long? DiagnosticStepId = null,
    string? CreatedByName = null,
    IReadOnlyList<CaseHypothesisEvidenceDto>? HypothesisRelations = null
);

public record CaseHypothesisEvidenceDto(
    long Id,
    long EvidenceId,
    long HypothesisId,
    string? HypothesisTitle,
    string RelationType, // Supports, Contradicts, Inconclusive, Confirms
    string? Justification,
    long CreatedBy,
    string? CreatedByName,
    DateTime CreatedAt
);

public record AttachmentDto(
    long Id,
    string EntityType,
    long EntityId,
    string FileName,
    string MimeType,
    ulong SizeBytes,
    string Sha256,
    string StorageKey,
    string Confidentiality,
    long UploadedBy,
    DateTime UploadedAt
);
