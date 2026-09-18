using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IIntegrationRepository
{
    Task<IReadOnlyList<Integration>> GetAllIntegrationsAsync(CancellationToken ct = default);
    Task<Integration?> GetIntegrationByIdAsync(long id, CancellationToken ct = default);
    Task<long> AddIntegrationAsync(Integration integration, CancellationToken ct = default);
    Task UpdateIntegrationAsync(Integration integration, CancellationToken ct = default);

    Task<IReadOnlyList<IntegrationRun>> GetRunsByIntegrationIdAsync(long integrationId, CancellationToken ct = default);
    Task<long> AddRunAsync(IntegrationRun run, CancellationToken ct = default);
}