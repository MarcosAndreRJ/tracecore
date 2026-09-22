using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface ICaseService
{
    Task<CaseDto> OpenCaseAsync(OpenCaseCommand command, long? currentUserId = null, CancellationToken ct = default);
    Task<CaseDto?> GetCaseByIdAsync(long id, CancellationToken ct = default);
    Task<CaseDto?> GetCaseByNumberAsync(ulong caseNumber, CancellationToken ct = default);
    Task<IReadOnlyList<CaseDto>> GetAllCasesAsync(int limit = 50, CancellationToken ct = default);
    Task UpdateNormalizedSummaryAsync(long caseId, string? normalizedSummary, long? currentUserId = null, CancellationToken ct = default);
    Task<CaseIterationDto> ReopenCaseAsync(long caseId, string reason, long reopenedBy, CancellationToken ct = default);
    Task AddTagAsync(long caseId, string tagName, long? currentUserId = null, CancellationToken ct = default);
    Task RemoveTagAsync(long caseId, string tagName, CancellationToken ct = default);
    Task AddSymptomAsync(long caseId, string symptomText, long? currentUserId = null, CancellationToken ct = default);

    // Apoio ao preenchimento da UI (catálogo e clientes)
    Task<IReadOnlyList<ClientDto>> GetClientsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductVersionDto>> GetVersionsByProductAsync(long productId, CancellationToken ct = default);
    Task<IReadOnlyList<EnvironmentDto>> GetEnvironmentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ComponentDto>> GetComponentsAsync(long? productId = null, CancellationToken ct = default);
}
