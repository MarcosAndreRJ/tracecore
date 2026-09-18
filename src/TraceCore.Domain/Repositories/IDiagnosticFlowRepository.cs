using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IDiagnosticFlowRepository
{
    Task<IReadOnlyList<DiagnosticFlow>> GetAllFlowsAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<DiagnosticFlow?> GetFlowByIdAsync(long id, CancellationToken ct = default);
    Task<DiagnosticFlow?> GetFlowByCodeAsync(string code, CancellationToken ct = default);
    Task<long> AddFlowAsync(DiagnosticFlow flow, CancellationToken ct = default);
    Task UpdateFlowAsync(DiagnosticFlow flow, CancellationToken ct = default);
    Task<bool> DeleteFlowAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<DiagnosticFlowHypothesis>> GetHypothesesByFlowIdAsync(long flowId, CancellationToken ct = default);
    Task<long> AddHypothesisAsync(DiagnosticFlowHypothesis hypothesis, CancellationToken ct = default);
    Task<bool> DeleteHypothesisAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<DiagnosticCheck>> GetChecksByFlowIdAsync(long flowId, CancellationToken ct = default);
    Task<DiagnosticCheck?> GetCheckByIdAsync(long id, CancellationToken ct = default);
    Task<long> AddCheckAsync(DiagnosticCheck check, CancellationToken ct = default);
    Task<bool> DeleteCheckAsync(long id, CancellationToken ct = default);

    Task<long> AddCheckOptionAsync(DiagnosticCheckOption option, CancellationToken ct = default);
    Task<long> AddCheckImpactAsync(DiagnosticCheckImpact impact, CancellationToken ct = default);
}
