using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface ICaseRepository
{
    /// <summary>
    /// Reserva o próximo número de caso de forma atômica e estável,
    /// sem reaproveitamento mesmo em falhas parciais de transação.
    /// </summary>
    Task<ulong> NextCaseNumberAsync(CancellationToken ct = default);

    Task<long> AddAsync(Case @case, CancellationToken ct = default);
    Task<Case?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Case?> GetByCaseNumberAsync(ulong caseNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Case>> GetAllAsync(int limit = 50, CancellationToken ct = default);
    Task UpdateNormalizedSummaryAsync(long caseId, string? normalizedSummary, long? updatedBy, CancellationToken ct = default);
    Task UpdateCaseResolutionStatusAsync(long caseId, string status, string rootCauseStatus, System.DateTime resolvedAt, long resolvedBy, CancellationToken ct = default);
    Task UpdateComponentRelationAsync(long caseId, long componentId, string relationType, CancellationToken ct = default);

    Task<IReadOnlyList<CaseIteration>> GetIterationsByCaseIdAsync(long caseId, CancellationToken ct = default);
    Task<CaseIteration?> GetCurrentIterationAsync(long caseId, CancellationToken ct = default);
    Task<long> AddIterationAsync(CaseIteration iteration, CancellationToken ct = default);
    Task UpdateIterationStatusAsync(long iterationId, string status, System.DateTime? closedAt, CancellationToken ct = default);
    Task UpdateCaseReopenStatusAsync(long caseId, string status, long reopenedBy, CancellationToken ct = default);
}

