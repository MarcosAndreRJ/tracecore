using System.Threading;
using System.Threading.Tasks;

namespace TraceCore.Application.Services;

public interface IAuditService
{
    Task RecordAsync(
        string action,
        string entityType,
        string entityId,
        long? actorUserId = null,
        string actorType = "User",
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null,
        object? before = null,
        object? after = null,
        object? metadata = null,
        CancellationToken ct = default);
}
