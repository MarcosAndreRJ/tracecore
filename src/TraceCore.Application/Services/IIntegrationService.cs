using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IIntegrationService
{
    Task<IReadOnlyList<IntegrationDto>> GetIntegrationsAsync(CancellationToken ct = default);
    Task<IntegrationDto?> GetIntegrationByIdAsync(long id, CancellationToken ct = default);
    Task<long> CreateIntegrationAsync(CreateIntegrationCommand command, CancellationToken ct = default);
    Task UpdateIntegrationStatusAsync(long id, string status, long? updatedBy, CancellationToken ct = default);
    Task<long> RegisterRunAsync(RegisterIntegrationRunCommand command, CancellationToken ct = default);
}