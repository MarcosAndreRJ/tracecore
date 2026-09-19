using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface ISearchableContentRepository
{
    Task<long> UpsertAsync(SearchableContentEntry entry, CancellationToken ct = default);
    Task<SearchableContentEntry?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SearchableContentEntry?> GetBySourceAsync(string sourceType, long sourceId, long? sourceVersionId, CancellationToken ct = default);
    Task<(IReadOnlyList<SearchableContentEntry> Items, int TotalCount)> SearchAsync(
        string? sourceType = null,
        string? validationStatus = null,
        string? qualityStatus = null,
        string? visibility = null,
        long? clientId = null,
        long? productId = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, int>> GetCountByQualityStatusAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, int>> GetCountBySourceTypeAsync(CancellationToken ct = default);

    // Fase 13 (M12): recuperação assistida por IA
    Task<IReadOnlyList<SearchableContentEntry>> GetRagCandidatesAsync(
        IReadOnlyList<string> visibilities,
        string? searchTerm,
        int limit,
        CancellationToken ct = default);

    Task<IReadOnlyList<SearchableContentEntry>> GetPendingEmbeddingAsync(
        string embeddingModel,
        int limit,
        CancellationToken ct = default);

    Task UpdateEmbeddingAsync(
        long id,
        string? embeddingVectorJson,
        string embeddingModel,
        DateTime? generatedAt,
        CancellationToken ct = default);
}
