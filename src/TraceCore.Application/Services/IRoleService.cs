using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken ct = default);
    Task<RoleDto?> GetRoleByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default);
    Task AssignRolePermissionsAsync(long roleId, IEnumerable<long> permissionIds, long? currentUserId = null, CancellationToken ct = default);
}
