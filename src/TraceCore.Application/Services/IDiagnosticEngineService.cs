using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IDiagnosticEngineService
{
    Task<DiagnosticEngineStateDto> GetEngineStateAsync(long caseId, CancellationToken ct = default);
    Task<DiagnosticFlowDto?> SuggestFlowForCaseAsync(long caseId, CancellationToken ct = default);
    Task<IReadOnlyList<DiagnosticFlowDto>> GetAllFlowsAsync(bool activeOnly = true, CancellationToken ct = default);
    Task StartFlowAsync(long caseId, long flowId, long userId, CancellationToken ct = default);
    Task AnswerCheckAsync(long caseId, long checkId, long optionId, string? evidenceText, long userId, CancellationToken ct = default);
    Task IgnoreRecommendationAsync(long caseId, long checkId, string reason, long userId, CancellationToken ct = default);

    // Gestão e Administração do Grafo (Fase 9 / Bloco 5)
    Task<DiagnosticFlowDetailsDto?> GetFlowDetailsAsync(long flowId, CancellationToken ct = default);
    Task<long> CreateFlowAsync(CreateDiagnosticFlowCommand command, long userId, CancellationToken ct = default);
    Task<long> AddCheckAsync(CreateDiagnosticCheckCommand command, CancellationToken ct = default);
    Task<long> AddCheckOptionWithImpactsAsync(long checkId, string optionText, int orderNo, List<(long HypothesisId, string ImpactType, decimal Weight)> impacts, CancellationToken ct = default);
    Task<long> AddFlowHypothesisAsync(long flowId, string title, string? description, long? componentId, CancellationToken ct = default);
    Task DeleteFlowAsync(long flowId, CancellationToken ct = default);
}
