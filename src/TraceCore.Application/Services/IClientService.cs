using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IClientService
{
    Task<IReadOnlyList<ClientDto>> GetAllClientsAsync(CancellationToken ct = default);
    Task<ClientDetailsDto?> GetClientDetailsAsync(long clientId, CancellationToken ct = default);
    Task<long> CreateClientAsync(CreateClientRequest request, long? currentUserId = null, CancellationToken ct = default);
    Task UpdateClientAsync(long clientId, UpdateClientRequest request, long? currentUserId = null, CancellationToken ct = default);

    Task<long> AddUnitAsync(long clientId, CreateClientUnitRequest request, long? currentUserId = null, CancellationToken ct = default);
    Task<long> AddTechnicalContextAsync(long clientId, CreateTechnicalContextRequest request, long? currentUserId = null, CancellationToken ct = default);
}
