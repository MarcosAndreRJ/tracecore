using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IKnowledgeRepository
{
    Task<KnowledgeItem?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<KnowledgeItem?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<long> CreateItemAsync(KnowledgeItem item, CancellationToken ct = default);
    Task UpdateItemAsync(KnowledgeItem item, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeItem>> SearchAsync(
        string? search = null,
        long? productId = null,
        string? status = null,
        string? provenance = null,
        string? category = null,
        CancellationToken ct = default);

    Task<KnowledgeVersion?> GetVersionByIdAsync(long versionId, CancellationToken ct = default);
    Task<KnowledgeVersion?> GetVersionByItemAndNumberAsync(long itemId, int versionNo, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeVersion>> GetVersionsByItemIdAsync(long itemId, CancellationToken ct = default);
    Task<long> CreateVersionAsync(KnowledgeVersion version, CancellationToken ct = default);
    Task UpdateVersionAsync(KnowledgeVersion version, CancellationToken ct = default);

    Task<IReadOnlyList<KnowledgeApplicability>> GetApplicabilitiesByItemIdAsync(long itemId, CancellationToken ct = default);
    Task<long> AddApplicabilityAsync(KnowledgeApplicability applicability, CancellationToken ct = default);
    Task RemoveApplicabilityAsync(long applicabilityId, CancellationToken ct = default);

    Task<IReadOnlyList<KnowledgeStep>> GetStepsByVersionIdAsync(long versionId, CancellationToken ct = default);
    Task AddStepsAsync(IEnumerable<KnowledgeStep> steps, CancellationToken ct = default);

    Task<IReadOnlyList<KnowledgeSymptom>> GetSymptomsByVersionIdAsync(long versionId, CancellationToken ct = default);
    Task AddSymptomsAsync(IEnumerable<KnowledgeSymptom> symptoms, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetTechnologiesByItemIdAsync(long itemId, CancellationToken ct = default);
    Task SetTechnologiesAsync(long itemId, IEnumerable<string> technologyNames, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetTagsByItemIdAsync(long itemId, CancellationToken ct = default);
    Task SetTagsAsync(long itemId, IEnumerable<string> tagNames, CancellationToken ct = default);

    Task<long> RecordUsageAsync(KnowledgeUsage usage, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeUsage>> GetUsagesByItemIdAsync(long itemId, CancellationToken ct = default);

    Task<(int TotalCount, int PublishedCount, int InReviewCount, int StaleCount)> GetDashboardCountsAsync(CancellationToken ct = default);
}
