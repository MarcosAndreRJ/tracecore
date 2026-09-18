using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IAuthenticationService
{
    Task<LoginResultDto> LoginAsync(string email, string password, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
    Task LogoutAsync(long userId, long? sessionId = null, string? ipAddress = null, CancellationToken ct = default);
}
