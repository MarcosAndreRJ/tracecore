using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IContentPreparationService
{
    Task<SearchableContentEntryDto> PrepareKnowledgeItemAsync(long knowledgeItemId, long? currentUserId = null, CancellationToken ct = default);
    Task<SearchableContentEntryDto> PrepareCaseAsync(long caseId, long? currentUserId = null, CancellationToken ct = default);
    Task<int> SyncAllPublishedKnowledgeAsync(long? currentUserId = null, CancellationToken ct = default);
    Task<int> SyncAllResolvedCasesAsync(long? currentUserId = null, CancellationToken ct = default);
    Task<ContentSearchResultDto> SearchAsync(SearchableContentFilterDto filter, CancellationToken ct = default);
    Task<ContentQualityMetricsDto> GetMetricsAsync(CancellationToken ct = default);
}
