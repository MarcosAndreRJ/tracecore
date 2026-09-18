using System;

namespace TraceCore.Application.DTOs;

public record ResolveCaseCommand(
    long CaseId,
    string ResolutionSummary,
    string ValidationSummary,
    long? RootCauseId = null,
    string? NewRootCauseName = null,
    string? NewRootCauseCategory = null,
    bool RootCauseConfirmed = false,
    long? ResponsibleComponentId = null,
    long? ResponsibleDepartmentId = null,
    string ResolutionType = "Definitive",
    string RecurrenceRisk = "Low",
    string? RecurrenceNotes = null,
    string? PreventiveActions = null,
    int? EffortMinutes = null
);

public record CaseResolutionDto(
    long Id,
    long CaseId,
    string ResolutionSummary,
    string ValidationSummary,
    long? RootCauseId,
    string? RootCauseName,
    string? RootCauseCode,
    string? RootCauseCategory,
    bool RootCauseConfirmed,
    long? ResponsibleDepartmentId,
    string? ResponsibleDepartmentName,
    long? ResponsibleComponentId,
    string? ResponsibleComponentName,
    string ResolutionType,
    string RecurrenceRisk,
    string? RecurrenceNotes,
    string? PreventiveActions,
    int? EffortMinutes,
    long ResolvedBy,
    string? ResolvedByName,
    DateTime ResolvedAt,
    double ElapsedMinutes,
    int TotalDiagnosticDurationSeconds,
    int FailedAttemptsCount,
    int SuccessfulAttemptsCount,
    int TotalHypothesesTestedCount
);

public record RootCauseDto(
    long Id,
    string? Code,
    string Name,
    string? Category,
    string? Description
);
