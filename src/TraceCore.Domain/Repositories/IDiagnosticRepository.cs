using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

/// <summary>
/// Contrato de persistência para sessões de diagnóstico, hipóteses e passos investigativos (M04).
/// </summary>
public interface IDiagnosticRepository
{
    Task<DiagnosticSession?> GetOpenSessionByCaseIdAsync(long caseId, CancellationToken ct = default);
    Task<long> CreateSessionAsync(DiagnosticSession session, CancellationToken ct = default);
    Task<DiagnosticSession?> GetSessionByIdAsync(long sessionId, CancellationToken ct = default);

    Task<long> AddHypothesisAsync(CaseHypothesis hypothesis, CancellationToken ct = default);
    Task<CaseHypothesis?> GetHypothesisByIdAsync(long hypothesisId, CancellationToken ct = default);
    Task<IReadOnlyList<CaseHypothesis>> GetHypothesesByCaseIdAsync(long caseId, CancellationToken ct = default);
    Task UpdateHypothesisAsync(CaseHypothesis hypothesis, CancellationToken ct = default);

    Task<int> GetNextStepSequenceNoAsync(long sessionId, CancellationToken ct = default);
    Task<long> AddStepAsync(DiagnosticStep step, CancellationToken ct = default);
    Task<DiagnosticStep?> GetStepByIdAsync(long stepId, CancellationToken ct = default);
    Task<IReadOnlyList<DiagnosticStep>> GetStepsBySessionIdAsync(long sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<DiagnosticStep>> GetStepsByCaseIdAsync(long caseId, CancellationToken ct = default);
    Task<IReadOnlyList<DiagnosticStep>> GetStepsByHypothesisIdAsync(long hypothesisId, CancellationToken ct = default);

    Task<long> AddEvidenceAsync(CaseEvidence evidence, CancellationToken ct = default);
    Task<CaseEvidence?> GetEvidenceByIdAsync(long evidenceId, CancellationToken ct = default);
    Task<IReadOnlyList<CaseEvidence>> GetEvidencesByCaseIdAsync(long caseId, CancellationToken ct = default);
    Task<IReadOnlyList<CaseEvidence>> GetEvidencesByHypothesisIdAsync(long hypothesisId, CancellationToken ct = default);
    Task AddHypothesisEvidenceRelationAsync(CaseHypothesisEvidence relation, CancellationToken ct = default);
    Task<IReadOnlyList<CaseHypothesisEvidence>> GetHypothesisRelationsByEvidenceIdAsync(long evidenceId, CancellationToken ct = default);
}
