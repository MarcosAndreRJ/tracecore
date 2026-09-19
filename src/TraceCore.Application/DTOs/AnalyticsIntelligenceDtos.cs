using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

/// <summary>
/// Fase 16 (Inteligência Analítica): DTOs para consultas analíticas determinísticas.
/// Rastreabilidade estrita (§31 do documento de visão): todo indicador traz número real + tamanho de amostra.
/// </summary>

public record TrendAfterVersionDto(
    long ProductVersionId,
    string VersionLabel,
    DateTime? ReleasedAt,
    int BeforeCount,
    int AfterCount,
    double? PercentageChange,
    int TotalSample,
    int IntervalDays,
    bool HasSufficientData,
    string Observation
);

public record ComponentAssociationItemDto(
    long ComponentId,
    string ComponentName,
    int CaseCount,
    double Percentage
);

public record ComponentAssociationDto(
    int TotalCases,
    IReadOnlyList<ComponentAssociationItemDto> Components,
    bool HasSufficientData,
    string Observation
);

public record SolutionEffectivenessDto(
    long KnowledgeItemId,
    string SolutionTitle,
    double? MedianMttrWithMinutes,
    int SampleWithCount,
    double? MedianMttrWithoutMinutes,
    int SampleWithoutCount,
    double? MttrReductionPercentage,
    bool HasSufficientData,
    string TechnicalScope,
    string Observation
);
