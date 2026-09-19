using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TraceCore.Domain.Repositories;

public record AnalyticsFilterCriteria
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public long? ClientId { get; init; }
    public long? ClientUnitId { get; init; }
    public long? ProductId { get; init; }
    public long? ProductVersionId { get; init; }
    public long? EnvironmentId { get; init; }
    public long? ComponentId { get; init; }
    public long? DepartmentId { get; init; }
    public string? Severity { get; init; }
    public string? Status { get; init; }
}

public record OverviewRawMetrics
{
    public int TotalCases { get; init; }
    public int OpenCases { get; init; }
    public int ResolvedCases { get; init; }
    public int RecurrentCases { get; init; }
    public int UnconfirmedRootCauseCases { get; init; }
    public int UndocumentedKnowledgeCases { get; init; }
}

public record TimeEvolutionRawItem
{
    public DateTime DateBucket { get; init; }
    public int OpenedCount { get; init; }
    public int ResolvedCount { get; init; }
}

public record EntityCountRawItem
{
    public long? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? SecondaryLabel { get; init; }
    public int Count { get; init; }
}

public record AttentionCaseRawItem
{
    public long Id { get; init; }
    public ulong CaseNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public DateTime OpenedAt { get; init; }
    public int ActiveDays { get; init; }
    public int IterationCount { get; init; }
    public bool HasRootCause { get; init; }
    public bool HasKnowledge { get; init; }
    public string AttentionReason { get; init; } = string.Empty;
}

public record DepartmentRawMetrics
{
    public long DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public int ActiveCasesCount { get; init; }
    public int ResolvedCasesCount { get; init; }
    public int ReopenedCasesCount { get; init; }
    public int RecurrentCasesCount { get; init; }
    public int KnowledgeCreatedCount { get; init; }
    public int DiagnosticStepsCount { get; init; }
}

public record UserRawMetrics
{
    public long UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public int OpenedCasesCount { get; init; }
    public int ResolvedCasesCount { get; init; }
    public int DiagnosticStepsCount { get; init; }
    public int HypothesesCreatedCount { get; init; }
    public int EvidencesCreatedCount { get; init; }
    public int KnowledgeAuthoredCount { get; init; }
    public int KnowledgeUsedCount { get; init; }
    public string? TopProduct { get; init; }
    public string? TopComponent { get; init; }
}

public record KnowledgeRawMetrics
{
    public int TotalPublished { get; init; }
    public int TotalDeprecated { get; init; }
    public int NeverReviewedCount { get; init; }
    public int ReviewOverdueCount { get; init; }
    public int TotalUsagesCount { get; init; }
    public int WorkedUsagesCount { get; init; }
    public int PartiallyWorkedUsagesCount { get; init; }
    public int DidNotWorkUsagesCount { get; init; }
    public int InconclusiveUsagesCount { get; init; }
}

public interface IManagementAnalyticsRepository
{
    Task<OverviewRawMetrics> GetOverviewMetricsAsync(AnalyticsFilterCriteria criteria, CancellationToken ct = default);
    Task<IReadOnlyList<double>> GetResolvedIterationDurationsMinutesAsync(AnalyticsFilterCriteria criteria, CancellationToken ct = default);
    Task<IReadOnlyList<TimeEvolutionRawItem>> GetTimeEvolutionAsync(AnalyticsFilterCriteria criteria, string grouping = "day", CancellationToken ct = default);
    Task<IReadOnlyList<EntityCountRawItem>> GetTopProductsAsync(AnalyticsFilterCriteria criteria, int limit = 5, CancellationToken ct = default);
    Task<IReadOnlyList<EntityCountRawItem>> GetTopComponentsAsync(AnalyticsFilterCriteria criteria, int limit = 5, CancellationToken ct = default);
    Task<IReadOnlyList<EntityCountRawItem>> GetTopRootCausesAsync(AnalyticsFilterCriteria criteria, int limit = 5, CancellationToken ct = default);
    Task<IReadOnlyList<EntityCountRawItem>> GetTopRecurrencesAsync(AnalyticsFilterCriteria criteria, int limit = 5, CancellationToken ct = default);
    Task<IReadOnlyList<AttentionCaseRawItem>> GetAttentionCasesAsync(AnalyticsFilterCriteria criteria, int limit = 10, CancellationToken ct = default);
    
    Task<IReadOnlyList<DepartmentRawMetrics>> GetDepartmentMetricsAsync(AnalyticsFilterCriteria criteria, CancellationToken ct = default);
    Task<IReadOnlyList<double>> GetDepartmentIterationDurationsMinutesAsync(long departmentId, AnalyticsFilterCriteria criteria, CancellationToken ct = default);
    
    Task<IReadOnlyList<UserRawMetrics>> GetUserMetricsAsync(AnalyticsFilterCriteria criteria, CancellationToken ct = default);
    
    Task<KnowledgeRawMetrics> GetKnowledgeMetricsAsync(AnalyticsFilterCriteria criteria, CancellationToken ct = default);
    Task<IReadOnlyList<EntityCountRawItem>> GetTopUsedKnowledgeAsync(AnalyticsFilterCriteria criteria, int limit = 5, CancellationToken ct = default);

    // Métodos determinísticos de Inteligência Analítica (Fase 16)
    Task<(string VersionLabel, DateTime? ReleasedAt, int BeforeCount, int AfterCount)> GetTrendAfterVersionRawAsync(long productVersionId, long? rootCauseId, string? errorCode, long? componentId, int intervalDays, CancellationToken ct = default);
    Task<(int TotalCases, IReadOnlyList<(long ComponentId, string ComponentName, int Count)> ComponentCounts)> GetComponentAssociationRawAsync(AnalyticsFilterCriteria criteria, long? rootCauseId, CancellationToken ct = default);
    Task<(string SolutionTitle, string ScopeLabel, IReadOnlyList<double> DurationsWithMinutes, IReadOnlyList<double> DurationsWithoutMinutes)> GetSolutionEffectivenessRawAsync(long knowledgeItemId, AnalyticsFilterCriteria criteria, CancellationToken ct = default);
}
