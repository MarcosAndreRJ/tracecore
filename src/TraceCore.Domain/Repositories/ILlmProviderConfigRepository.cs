using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface ILlmProviderConfigRepository
{
    Task<LlmProviderConfig?> GetActiveByPurposeAsync(string purpose, CancellationToken ct = default);
    Task<LlmProviderConfig?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<LlmProviderConfig>> GetAllAsync(CancellationToken ct = default);
    Task<LlmProviderConfig?> GetByPurposeAndProviderAsync(string purpose, string providerCode, CancellationToken ct = default);
    Task<long> AddAsync(LlmProviderConfig config, CancellationToken ct = default);
    Task UpdateAsync(LlmProviderConfig config, CancellationToken ct = default);
}