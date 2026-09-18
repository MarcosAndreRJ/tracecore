using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(Role role, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> GetRolePermissionsAsync(long roleId, CancellationToken ct = default);
    Task SetRolePermissionsAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken ct = default);
}
