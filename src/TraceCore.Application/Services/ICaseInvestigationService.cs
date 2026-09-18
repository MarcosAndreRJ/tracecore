using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

/// <summary>
/// Contrato para serviços de investigação do caso e linha de diagnóstico (M04).
/// </summary>
public interface ICaseInvestigationService
{
    Task<CaseHypothesisDto> RegisterHypothesisAsync(RegisterHypothesisCommand command, long? currentUserId = null, CancellationToken ct = default);
    Task<DiagnosticStepDto> RegisterDiagnosticStepAsync(RegisterDiagnosticStepCommand command, long? currentUserId = null, CancellationToken ct = default);
    Task<CaseHypothesisDto> EvaluateHypothesisAsync(EvaluateHypothesisCommand command, long? currentUserId = null, CancellationToken ct = default);
    Task<CaseInvestigationTimelineDto> GetInvestigationTimelineAsync(long caseId, CancellationToken ct = default);
    Task<IReadOnlyList<CaseHypothesisDto>> GetHypothesesByCaseIdAsync(long caseId, CancellationToken ct = default);

    Task<CaseEvidenceDto> RecordEvidenceAsync(RecordEvidenceCommand command, long currentUserId, CancellationToken ct = default);
    Task<IReadOnlyList<CaseEvidenceDto>> GetEvidencesByCaseIdAsync(long caseId, CancellationToken ct = default);
    Task<IReadOnlyList<CaseEvidenceDto>> GetEvidencesByHypothesisIdAsync(long hypothesisId, CancellationToken ct = default);
}
