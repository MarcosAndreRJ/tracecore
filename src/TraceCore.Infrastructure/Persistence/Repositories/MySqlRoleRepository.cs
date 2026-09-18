using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlRoleRepository : IRoleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlRoleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Role?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT id, name, description FROM roles WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Role>(sql, new { Id = id });
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        const string sql = "SELECT id, name, description FROM roles WHERE name = @Name;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Role>(sql, new { Name = name.Trim() });
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT id, name, description FROM roles ORDER BY name ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<Role>(sql);
        return list.ToList();
    }

    public async Task<long> AddAsync(Role role, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO roles (name, description) VALUES (@Name, @Description);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, role);
        role.Id = id;
        return id;
    }

    public async Task<IReadOnlyList<Permission>> GetRolePermissionsAsync(long roleId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT p.id, p.code, p.description 
            FROM permissions p
            INNER JOIN role_permissions rp ON rp.permission_id = p.id
            WHERE rp.role_id = @RoleId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<Permission>(sql, new { RoleId = roleId });
        return list.ToList();
    }

    public async Task SetRolePermissionsAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync("DELETE FROM role_permissions WHERE role_id = @RoleId;", new { RoleId = roleId }, tx);

        var list = permissionIds.Distinct().ToList();
        if (list.Count > 0)
        {
            const string insertSql = "INSERT INTO role_permissions (role_id, permission_id) VALUES (@RoleId, @PermissionId);";
            foreach (var permId in list)
            {
                await conn.ExecuteAsync(insertSql, new { RoleId = roleId, PermissionId = permId }, tx);
            }
        }

        tx.Commit();
    }
}
