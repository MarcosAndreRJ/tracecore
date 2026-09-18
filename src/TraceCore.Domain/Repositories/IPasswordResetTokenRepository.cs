using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<long> AddAsync(PasswordResetToken token, CancellationToken ct = default);
    Task MarkAsUsedAsync(long id, CancellationToken ct = default);
    Task InvalidateAllForUserAsync(long userId, CancellationToken ct = default);
}
