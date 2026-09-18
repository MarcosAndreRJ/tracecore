using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface ICaseResolutionService
{
    Task<CaseResolutionDto> ResolveCaseAsync(ResolveCaseCommand command, long currentUserId, CancellationToken ct = default);
    Task<CaseResolutionDto?> GetResolutionByCaseIdAsync(long caseId, CancellationToken ct = default);
    Task<IReadOnlyList<RootCauseDto>> GetRootCausesAsync(CancellationToken ct = default);
}
