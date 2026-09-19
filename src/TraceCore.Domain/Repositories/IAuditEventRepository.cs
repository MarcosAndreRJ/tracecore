using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IAuditEventRepository
{
    Task<long> AddAsync(AuditEvent auditEvent, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEvent>> GetRecentAsync(int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEvent>> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default);
    Task<(IReadOnlyList<AuditEvent> Items, int TotalCount)> SearchAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        long? actorUserId = null,
        string? action = null,
        string? entityType = null,
        string? entityId = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default);
}
