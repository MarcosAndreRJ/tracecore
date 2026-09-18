using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Client?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Client?> GetByExternalCrmIdAsync(string externalCrmId, CancellationToken ct = default);
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(Client client, CancellationToken ct = default);
    Task UpdateAsync(Client client, CancellationToken ct = default);

    // Unidades de Cliente
    Task<IReadOnlyList<ClientUnit>> GetUnitsByClientIdAsync(long clientId, CancellationToken ct = default);
    Task<ClientUnit?> GetUnitByIdAsync(long unitId, CancellationToken ct = default);
    Task<long> AddUnitAsync(ClientUnit unit, CancellationToken ct = default);
    Task UpdateUnitAsync(ClientUnit unit, CancellationToken ct = default);

    // Contextos Técnicos de Cliente
    Task<IReadOnlyList<ClientTechnicalContext>> GetTechnicalContextsByClientIdAsync(long clientId, CancellationToken ct = default);
    Task<ClientTechnicalContext?> GetTechnicalContextByIdAsync(long contextId, CancellationToken ct = default);
    Task<long> AddTechnicalContextAsync(ClientTechnicalContext context, CancellationToken ct = default);
    Task UpdateTechnicalContextAsync(ClientTechnicalContext context, CancellationToken ct = default);
}
