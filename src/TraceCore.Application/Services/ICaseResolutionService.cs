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

    // Administração do catálogo corporativo de causas raízes (Cases/RootCauses).
    Task<long> CreateRootCauseAsync(string name, string? code, string? category, string? description, CancellationToken ct = default);
    Task UpdateRootCauseAsync(long id, string name, string? code, string? category, string? description, CancellationToken ct = default);
    Task<int> DeleteRootCauseAsync(long id, CancellationToken ct = default);
    Task<int> CountResolutionsUsingRootCauseAsync(long id, CancellationToken ct = default);

    // Aplica a mesma resolução já registrada no caso de origem a cada caso vinculado por
    // "Causa Comum" que ainda esteja em aberto — ação explícita e separada do encerramento
    // principal (o vínculo em si não fecha os outros casos automaticamente).
    Task<IReadOnlyList<long>> ApplyResolutionToLinkedCasesAsync(long sourceCaseId, long currentUserId, CancellationToken ct = default);

    // Vínculo manual imediato: aplica a resolução de um caso já Resolvido ao caso atual
    // (usado quando o analista confirma explicitamente "fechar o caso atual também").
    Task<bool> ApplyResolutionFromRelatedCaseAsync(long caseId, long relatedResolvedCaseId, long currentUserId, CancellationToken ct = default);
}
