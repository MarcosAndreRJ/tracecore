using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

/// <summary>
/// Repositório dedicado ao versionamento inteligente de releases (Fase 1): itens de
/// alteração de cada ProductVersion, a associação N:N com casos (fixed-by/related by)
/// e a destinação (rollout) de versões para clientes. Separado de ICatalogRepository
/// para não sobrecarregar uma interface já extensa (mesma justificativa de
/// IProductTechnicalContextRepository).
/// </summary>
public interface IProductVersionManagementRepository
{
    // ===================== Alterações (ProductVersionChange) =====================

    Task<IReadOnlyList<ProductVersionChange>> GetChangesByVersionIdAsync(long productVersionId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductVersionChange>> GetFixChangesForProductAsync(long productId, int minReleaseOrderExclusive, CancellationToken ct = default);
    Task<ProductVersionChange?> GetChangeByIdAsync(long id, CancellationToken ct = default);
    Task<long> AddChangeAsync(ProductVersionChange change, CancellationToken ct = default);
    Task UpdateChangeAsync(ProductVersionChange change, CancellationToken ct = default);
    Task<bool> DeleteChangeAsync(long id, CancellationToken ct = default);

    // ============= Associação alteração <-> caso (ProductVersionChangeCase) =============

    Task<IReadOnlyList<ProductVersionChangeCase>> GetLinkedCasesByChangeIdAsync(long changeId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductVersionChangeCase>> GetChangesLinkedToCaseAsync(long caseId, CancellationToken ct = default);
    Task<bool> ChangeCaseLinkExistsAsync(long changeId, long caseId, string relationType, CancellationToken ct = default);
    Task AddChangeCaseLinkAsync(ProductVersionChangeCase link, CancellationToken ct = default);
    Task<bool> DeleteChangeCaseLinkAsync(long changeId, long caseId, string relationType, CancellationToken ct = default);

    // ===================== Destinação / rollout (ProductVersionAssignment) =====================

    Task<IReadOnlyList<ProductVersionAssignment>> GetAssignmentsByVersionIdAsync(long productVersionId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductVersionAssignment>> GetAssignmentsByClientIdAsync(long clientId, CancellationToken ct = default);
    Task<ProductVersionAssignment?> GetAssignmentByIdAsync(long id, CancellationToken ct = default);
    Task<long> AddAssignmentAsync(ProductVersionAssignment assignment, CancellationToken ct = default);
    Task UpdateAssignmentAsync(ProductVersionAssignment assignment, CancellationToken ct = default);
    Task<bool> DeleteAssignmentAsync(long id, CancellationToken ct = default);
    Task<bool> AssignmentExistsAsync(long productVersionId, long clientId, long? clientUnitId, CancellationToken ct = default);

    // ===================== Consultas Agregadas & Histórico (Fase 5) =====================

    Task<IReadOnlyDictionary<long, int>> GetCaseCountsByVersionIdsAsync(IEnumerable<long> versionIds, CancellationToken ct = default);
    Task<IReadOnlyList<VersionLinkedCaseDetailDb>> GetLinkedCaseDetailsByVersionIdAsync(long productVersionId, CancellationToken ct = default);
    Task<IReadOnlyList<ClientVersionCaseDb>> GetCasesByClientAndPeriodAsync(long clientId, long productId, DateTime from, DateTime? to, CancellationToken ct = default);
    Task<int> GetPostReleaseSimilarCasesCountAsync(long productId, long productVersionId, string? errorCode, long? componentId, DateTime releaseDate, CancellationToken ct = default);
}