using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IAttachmentRepository
{
    Task<long> AddAsync(Attachment attachment, CancellationToken ct = default);
    Task<Attachment?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<Attachment>> GetByEntityAsync(string entityType, long entityId, CancellationToken ct = default);
}
