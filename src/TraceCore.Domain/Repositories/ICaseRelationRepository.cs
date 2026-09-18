using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface ICaseRelationRepository
{
    Task<IReadOnlyList<CaseRelation>> GetRelationsByCaseIdAsync(long caseId, CancellationToken ct = default);
    Task SaveSimilarRelationsAsync(long sourceCaseId, IEnumerable<CaseRelation> relations, CancellationToken ct = default);
    Task<long> AddManualRelationAsync(CaseRelation relation, CancellationToken ct = default);
    Task<IReadOnlyList<Case>> GetPotentialSimilarCandidatesAsync(long excludeCaseId, long? productId, string? errorCode, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<DiagnosticStep>> GetSuccessfulDiagnosticStepsForCasesAsync(IEnumerable<long> caseIds, CancellationToken ct = default);
    Task<IReadOnlyList<CaseResolution>> GetResolutionsForCasesAsync(IEnumerable<long> caseIds, CancellationToken ct = default);
}
