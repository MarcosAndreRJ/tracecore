using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IKnowledgeService
{
    Task<long> CreateKnowledgeDraftAsync(CreateKnowledgeDraftCommand command, long userId, CancellationToken ct = default);
    Task<long> CreateDraftFromCaseAsync(CreateDraftFromCaseCommand command, long userId, CancellationToken ct = default);
    Task SubmitForReviewAsync(SubmitForReviewCommand command, long userId, CancellationToken ct = default);
    Task ApproveAndPublishAsync(ApproveAndPublishCommand command, long userId, CancellationToken ct = default);
    Task<int> CreateNewVersionAsync(CreateNewVersionCommand command, long userId, CancellationToken ct = default);
    Task DeprecateKnowledgeAsync(DeprecateKnowledgeCommand command, long userId, CancellationToken ct = default);
    Task ArchiveKnowledgeAsync(ArchiveKnowledgeCommand command, long userId, CancellationToken ct = default);
    Task<long> RecordUsageAsync(RecordKnowledgeUsageCommand command, long userId, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeItemSummaryDto>> SearchKnowledgeAsync(
        string? search = null,
        long? productId = null,
        string? status = null,
        string? provenance = null,
        string? category = null,
        CancellationToken ct = default);

    Task<KnowledgeDetailDto?> GetKnowledgeDetailAsync(long id, CancellationToken ct = default);
    Task<(int TotalCount, int PublishedCount, int InReviewCount, int StaleCount, string OverallSuccessRate)> GetKnowledgeDashboardMetricsAsync(CancellationToken ct = default);
}
