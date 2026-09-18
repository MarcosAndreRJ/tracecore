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
}
