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

    // Fase 05 — Teste de integração durante a investigação do caso: cria uma
    // IntegrationRun (RunContext=Diagnostic) + DiagnosticStep (AutomatedCheck) e,
    // opcionalmente, uma CaseEvidence (DiagnosticTest) vinculada à execução.
    Task<DiagnosticStepDto> TestIntegrationDuringInvestigationAsync(
        long caseId,
        long integrationId,
        long currentUserId,
        long? hypothesisId = null,
        bool recordAsEvidence = false,
        string? evidenceRelationType = null,
        CancellationToken ct = default);

    // Fase 05 — Teste de integração durante a validação da solução: cria uma
    // IntegrationRun (RunContext=SolutionValidation) + CaseEvidence (DiagnosticTest)
    // na iteração atual do caso.
    Task<CaseEvidenceDto> TestIntegrationForSolutionValidationAsync(
        long caseId,
        long integrationId,
        long currentUserId,
        CancellationToken ct = default);
}
