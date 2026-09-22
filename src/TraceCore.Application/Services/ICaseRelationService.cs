using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface ICaseRelationService
{
    Task<IReadOnlyList<CaseRelationDto>> ComputeSimilarCasesAsync(long caseId, CancellationToken ct = default);
    Task<CaseRelationsOverviewDto> GetCaseRelationsOverviewAsync(long caseId, CancellationToken ct = default);
    Task<long> CreateManualRelationAsync(CreateCaseRelationCommand command, long userId, CancellationToken ct = default);
    Task DeleteManualRelationAsync(long relationId, long userId, CancellationToken ct = default);

    // Pré-visualização durante a abertura do caso (mecanismo determinístico compartilhado
    // com ComputeSimilarCasesAsync — ver CaseRelationService). Não persiste nada: o caso
    // ainda não existe. Quando o caso for salvo, a computação "oficial" (persistida) roda
    // automaticamente na primeira visita aos Detalhes (GetCaseRelationsOverviewAsync).
    Task<IReadOnlyList<CaseRelationDto>> PreviewSimilarCasesAsync(CaseSimilarityDraftInput input, CancellationToken ct = default);
}
