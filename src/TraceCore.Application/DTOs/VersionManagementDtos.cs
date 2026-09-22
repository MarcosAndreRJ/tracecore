using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

/// <summary>
/// Item de release cadastrado em uma ProductVersion (Fase 1 — Versionamento Inteligente).
/// </summary>
public record ProductVersionChangeDto(
    long Id,
    long ProductVersionId,
    string? VersionLabel,
    string ChangeType,
    string ChangeTypeLabel,
    string Title,
    string? Description,
    long? ComponentId,
    string? ComponentName,
    string? ErrorCode,
    DateTime CreatedAt,
    long? CreatedBy,
    DateTime? UpdatedAt,
    long? UpdatedBy,
    IReadOnlyList<VersionChangeCaseLinkDto> LinkedCases
);

/// <summary>
/// Caso vinculado a um item de release (relation_type: FixedBy / Related e extensões futuras).
/// </summary>
public record VersionChangeCaseLinkDto(
    long ChangeId,
    long CaseId,
    ulong CaseNumber,
    string? CaseSummary,
    string? CaseStatus,
    string RelationType,
    string RelationTypeLabel,
    decimal? MatchScore,
    string? MatchedFactorsJson,
    long? LinkedBy,
    DateTime LinkedAt
);

/// <summary>
/// Dados de entrada para sugestão de casos a serem vinculados a uma alteração (Fase 3).
/// </summary>
public record VersionChangeCaseSuggestionInput(
    long? ProductVersionChangeId,
    long ProductVersionId,
    long ProductId,
    long? ComponentId,
    string? ErrorCode,
    string? Title,
    string? Description
);

/// <summary>
/// Sugestão de caso com pontuação determinística para resolução por correção de versão (Fase 3).
/// </summary>
public record VersionChangeCaseSuggestionDto(
    long CaseId,
    ulong CaseNumber,
    string CaseTitle,
    string CaseStatus,
    string? ProductVersionLabel,
    double Score,
    IReadOnlyList<string> MatchedFactors
);

/// <summary>
/// Destinação de uma ProductVersion para um cliente/unidade (rollout, Fase 1).
/// Criar um assignment (Planned/Scheduled) NÃO muda a versão corrente do cliente;
/// somente o deploy confirmado (status Deployed) atualiza ClientTechnicalContext.
/// </summary>
public record ProductVersionAssignmentDto(
    long Id,
    long ProductVersionId,
    string? VersionLabel,
    long ClientId,
    string? ClientName,
    long? ClientUnitId,
    string? ClientUnitName,
    string Status,
    string StatusLabel,
    DateTime PlannedAt,
    DateTime? ScheduledAt,
    DateTime? DeployedAt,
    string? Notes,
    DateTime CreatedAt,
    long? CreatedBy,
    DateTime? UpdatedAt,
    long? UpdatedBy
);

public record ClientActiveVersionDto(
    long? ProductVersionId,
    string? VersionLabel,
    long? EnvironmentId,
    string? EnvironmentName,
    bool IsAmbiguous,
    IReadOnlyList<AmbiguousEnvironmentDto> AmbiguousEnvironments,
    bool HasContext
);

public record AmbiguousEnvironmentDto(
    long? EnvironmentId,
    string? EnvironmentName,
    long? ProductVersionId,
    string? VersionLabel
);

public record PossibleFixSuggestionDto(
    long ChangeId,
    long ProductVersionId,
    string VersionLabel,
    string ChangeTitle,
    string? ChangeDescription,
    int LinkedCaseCount,
    decimal Score,
    IReadOnlyList<string> MatchedFactors
);

public record CaseConfirmedFixDto(
    long ChangeId,
    long ProductVersionId,
    string VersionLabel,
    string ChangeTitle,
    string RelationType,
    DateTime LinkedAt
);

public record CaseVersionContextDto(
    long? ProductVersionId,
    string? VersionLabel,
    int? ReleaseOrder,
    IReadOnlyList<CaseConfirmedFixDto> ConfirmedFixes,
    IReadOnlyList<PossibleFixSuggestionDto> PossibleFixes
);

/// <summary>
/// Visão detalhada de um caso vinculado a uma alteração da versão (Fase 5 - Aba Casos).
/// </summary>
public record VersionLinkedCaseDetailDto(
    long ChangeId,
    string ChangeTitle,
    string ChangeType,
    string ChangeTypeLabel,
    long CaseId,
    ulong CaseNumber,
    string CaseTitle,
    string CaseStatus,
    long? ClientId,
    string? ClientName,
    long? OccurredInVersionId,
    string? OccurredInVersionLabel,
    string RelationType,
    string RelationTypeLabel,
    DateTime LinkedAt,
    DateTime CaseOpenedAt
);

/// <summary>
/// Indicadores calculados estruturalmente para uma versão (Fase 5).
/// </summary>
public record VersionIndicatorsDto(
    long ProductVersionId,
    string VersionLabel,
    int ReleaseOrder,
    int CasesOccurredCount,
    int PublishedFixesCount,
    int LinkedCasesCount,
    int PlannedClientsCount,
    int ScheduledClientsCount,
    int DeployedClientsCount,
    int PendingClientsCount
);

/// <summary>
/// Quantidade factual de casos por versão para comparação global (Fase 5).
/// </summary>
public record VersionCaseCountDto(
    long ProductVersionId,
    string VersionLabel,
    int ReleaseOrder,
    int CaseCount
);

/// <summary>
/// Observação factual de recorrência pós-release para uma alteração de correção (Fase 5).
/// Não afirma causalidade: apenas dados objetivos.
/// </summary>
public record FixRecurrenceObservationDto(
    long ChangeId,
    string ChangeTitle,
    long ProductVersionId,
    string VersionLabel,
    string? ErrorCode,
    long? ComponentId,
    string? ComponentName,
    int HistoricalCasesCount,
    int DeployedClientsCount,
    int PostReleaseOccurrencesCount,
    string ObservationMessage
);

/// <summary>
/// Caso ocorrido durante um período de vigência de versão de um cliente (Fase 5).
/// </summary>
public record ClientPeriodCaseDto(
    long CaseId,
    ulong CaseNumber,
    string Title,
    string Status,
    string? ErrorCode,
    DateTime OpenedAt
);

/// <summary>
/// Período de vigência de versão de um cliente e seus casos ocorridos (Fase 5).
/// </summary>
public record ClientVersionPeriodDto(
    long? ProductVersionId,
    string? VersionLabel,
    long? EnvironmentId,
    string? EnvironmentName,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsCurrent,
    IReadOnlyList<ClientPeriodCaseDto> CasesOccurred
);

/// <summary>
/// Linha do tempo completa de versões de um cliente para um produto com casos por período (Fase 5).
/// </summary>
public record ClientVersionTimelineDto(
    long ClientId,
    string ClientName,
    long ProductId,
    string ProductName,
    long? ClientUnitId,
    string? ClientUnitName,
    IReadOnlyList<ClientVersionPeriodDto> Periods
);

/// <summary>
/// Resumo de versão para uso estruturado no Copiloto IA (Fase 5).
/// </summary>
public record VersionReleaseSummaryDto(
    long Id,
    string VersionLabel,
    int ReleaseOrder,
    DateTime? ReleasedAt,
    string Status
);

/// <summary>
/// Correção lançada em versão posterior entregue estruturada ao Copiloto IA (Fase 5).
/// </summary>
public record LaterVersionFixCopilotDto(
    long ChangeId,
    long ProductVersionId,
    string VersionLabel,
    int ReleaseOrder,
    string Title,
    string? Description,
    string? ErrorCode,
    string? ComponentName,
    int LinkedHistoricalCasesCount,
    IReadOnlyList<ulong> LinkedCaseNumbers,
    int DeployedClientsCount
);

/// <summary>
/// Contexto factual de versão de cliente entregue ao Copiloto IA (Fase 5).
/// O backend entrega os dados estruturados; o LLM apenas interpreta.
/// </summary>
public record ClientVersionCopilotContextDto(
    long ClientId,
    string ClientName,
    long ProductId,
    string ProductName,
    VersionReleaseSummaryDto? CurrentVersion,
    IReadOnlyList<VersionReleaseSummaryDto> PreviousVersions,
    IReadOnlyList<VersionReleaseSummaryDto> LaterVersions,
    IReadOnlyList<LaterVersionFixCopilotDto> LaterVersionFixes,
    IReadOnlyList<ProductVersionAssignmentDto> PlannedAssignments,
    string StatusSummary
);