using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface ILlmConfigurationService
{
    Task<IReadOnlyList<LlmProviderConfigDto>> GetConfigsAsync(CancellationToken ct = default);
    Task ActivateAsync(long configId, long? updatedBy, CancellationToken ct = default);
    Task UpdateModelAsync(long configId, string modelName, long? updatedBy, CancellationToken ct = default);
    bool IsCredentialConfigured(string providerCode);
}