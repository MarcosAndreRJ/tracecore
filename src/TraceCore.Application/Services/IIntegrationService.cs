using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Entities;

namespace TraceCore.Application.Services;

public interface IIntegrationService
{
    Task<IReadOnlyList<IntegrationDto>> GetIntegrationsAsync(CancellationToken ct = default);
    Task<IntegrationDto?> GetIntegrationByIdAsync(long id, CancellationToken ct = default);
    Task<long> CreateIntegrationAsync(CreateIntegrationCommand command, CancellationToken ct = default);
    Task UpdateIntegrationAsync(UpdateIntegrationCommand command, long? currentUserId = null, CancellationToken ct = default);
    Task UnlinkIntegrationFromProductAsync(long id, long? currentUserId = null, CancellationToken ct = default);
    Task LinkIntegrationToProductAsync(long id, long productId, long? currentUserId = null, CancellationToken ct = default);
    Task UpdateIntegrationStatusAsync(long id, string status, long? updatedBy, CancellationToken ct = default);
    Task ConfigureHealthCheckAsync(ConfigureIntegrationHealthCheckCommand command, CancellationToken ct = default);
    Task<long> RegisterRunAsync(RegisterIntegrationRunCommand command, CancellationToken ct = default);

    Task<IReadOnlyList<IntegrationType>> GetIntegrationTypesAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<long> CreateIntegrationTypeAsync(string code, string name, long? currentUserId = null, CancellationToken ct = default);
    Task DeactivateIntegrationTypeAsync(long id, long? currentUserId = null, CancellationToken ct = default);
}