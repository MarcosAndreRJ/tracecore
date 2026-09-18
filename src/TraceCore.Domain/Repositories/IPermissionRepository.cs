using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Permission?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(Permission permission, CancellationToken ct = default);
}
