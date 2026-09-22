using System;
using System.Collections.Generic;

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
    int? EffortMinutes = null,
    // Hipóteses do próprio caso apontadas como causa raiz real investigada (0..N).
    List<long>? RootCauseHypothesisIds = null,
    // Casos semelhantes ainda em aberto que o analista confirma serem o mesmo problema
    // (cria vínculo manual "CommonCause" — não fecha os casos automaticamente).
    List<long>? LinkedSimilarCaseIds = null
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
    int TotalHypothesesTestedCount,
    IReadOnlyList<CaseResolutionHypothesisDto>? RootCauseHypotheses = null
);

public record CaseResolutionHypothesisDto(
    long Id,
    string Title
);

public record RootCauseDto(
    long Id,
    string? Code,
    string Name,
    string? Category,
    string? Description
);
