using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IManagementAnalyticsService
{
    Task<ManagementOverviewDto> GetOverviewAnalyticsAsync(AnalyticsFilterDto filter, CancellationToken ct = default);
    Task<DepartmentAnalyticsDto> GetDepartmentAnalyticsAsync(AnalyticsFilterDto filter, CancellationToken ct = default);
    Task<UserAnalyticsDto> GetUserAnalyticsAsync(AnalyticsFilterDto filter, CancellationToken ct = default);
    Task<KnowledgeAnalyticsDto> GetKnowledgeAnalyticsAsync(AnalyticsFilterDto filter, CancellationToken ct = default);

    // Métodos determinísticos de Inteligência Analítica (Fase 16)
    Task<TrendAfterVersionDto> GetTrendAfterVersionAsync(long productVersionId, long? rootCauseId = null, string? errorCode = null, long? componentId = null, int intervalDays = 90, CancellationToken ct = default);
    Task<ComponentAssociationDto> GetComponentAssociationPercentageAsync(AnalyticsFilterDto filter, long? rootCauseId = null, CancellationToken ct = default);
    Task<SolutionEffectivenessDto> GetSolutionEffectivenessComparisonAsync(long knowledgeItemId, AnalyticsFilterDto filter, CancellationToken ct = default);
}
