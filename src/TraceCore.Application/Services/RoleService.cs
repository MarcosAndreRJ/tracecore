using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAuditService _auditService;

    public RoleService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IAuditService auditService)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken ct = default)
    {
        var roles = await _roleRepository.GetAllAsync(ct);
        var result = new List<RoleDto>();

        foreach (var role in roles)
        {
            var permissions = await _roleRepository.GetRolePermissionsAsync(role.Id, ct);
            result.Add(new RoleDto(
                role.Id,
                role.Name,
                role.Description,
                permissions.Select(p => new PermissionDto(p.Id, p.Code, p.Description)).ToList()
            ));
        }

        return result;
    }

    public async Task<RoleDto?> GetRoleByIdAsync(long id, CancellationToken ct = default)
    {
        var role = await _roleRepository.GetByIdAsync(id, ct);
        if (role == null) return null;

        var permissions = await _roleRepository.GetRolePermissionsAsync(role.Id, ct);
        return new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            permissions.Select(p => new PermissionDto(p.Id, p.Code, p.Description)).ToList()
        );
    }

    public async Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default)
    {
        var permissions = await _permissionRepository.GetAllAsync(ct);
        return permissions.Select(p => new PermissionDto(p.Id, p.Code, p.Description)).ToList();
    }

    public async Task AssignRolePermissionsAsync(long roleId, IEnumerable<long> permissionIds, long? currentUserId = null, CancellationToken ct = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, ct);
        if (role == null)
            throw new EntityNotFoundException("Papel", roleId);

        var permList = permissionIds.Distinct().ToList();
        var previousPerms = (await _roleRepository.GetRolePermissionsAsync(roleId, ct)).Select(p => p.Id).ToList();

        await _roleRepository.SetRolePermissionsAsync(roleId, permList, ct);

        // BR-004: Auditoria
        await _auditService.RecordAsync(
            action: "role.assign_permissions",
            entityType: "roles",
            entityId: roleId.ToString(),
            actorUserId: currentUserId,
            before: new { Permissions = previousPerms },
            after: new { Permissions = permList },
            ct: ct
        );
    }
}
