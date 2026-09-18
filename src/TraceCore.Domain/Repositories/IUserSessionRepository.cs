using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<long> AddAsync(UserSession session, CancellationToken ct = default);
    Task RevokeAsync(long id, CancellationToken ct = default);
    Task RevokeAllForUserAsync(long userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserSession>> GetActiveSessionsByUserIdAsync(long userId, CancellationToken ct = default);
}
