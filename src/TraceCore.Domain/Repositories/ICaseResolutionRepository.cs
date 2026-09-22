using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

/// <summary>
/// Contrato de persistência para causas raízes e resoluções de casos (Fase 5 / M04).
/// </summary>
public interface ICaseResolutionRepository
{
    Task<long> AddResolutionAsync(CaseResolution resolution, CancellationToken ct = default);
    Task<CaseResolution?> GetByCaseIdAsync(long caseId, CancellationToken ct = default);
    Task<CaseResolution?> GetByIterationIdAsync(long iterationId, CancellationToken ct = default);
    Task<IReadOnlyList<CaseResolution>> GetAllResolutionsByCaseIdAsync(long caseId, CancellationToken ct = default);
    Task<CaseResolution?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<long> AddRootCauseAsync(RootCause rootCause, CancellationToken ct = default);
    Task<RootCause?> GetRootCauseByIdAsync(long id, CancellationToken ct = default);
    Task<RootCause?> GetRootCauseByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<RootCause>> GetAllRootCausesAsync(CancellationToken ct = default);
    Task UpdateRootCauseAsync(RootCause rootCause, CancellationToken ct = default);
    Task<bool> DeleteRootCauseAsync(long id, CancellationToken ct = default);
    Task<int> CountResolutionsUsingRootCauseAsync(long rootCauseId, CancellationToken ct = default);

    Task SetResolutionHypothesesAsync(long resolutionId, IEnumerable<long> hypothesisIds, CancellationToken ct = default);
}
