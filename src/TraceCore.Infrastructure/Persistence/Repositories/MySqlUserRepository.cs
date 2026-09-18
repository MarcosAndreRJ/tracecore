using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlUserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlUserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, name, email, password_hash AS PasswordHash, status, 
                   last_login_at AS LastLoginAt, avatar_storage_key AS AvatarStorageKey, 
                   created_at AS CreatedAt, created_by AS CreatedBy, 
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy, 
                   row_version AS RowVersion 
            FROM users 
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var record = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
        if (record == null) return null;

        return MapDynamicToUser(record);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, name, email, password_hash AS PasswordHash, status, 
                   last_login_at AS LastLoginAt, avatar_storage_key AS AvatarStorageKey, 
                   created_at AS CreatedAt, created_by AS CreatedBy, 
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy, 
                   row_version AS RowVersion 
            FROM users 
            WHERE email = @Email;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var record = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { Email = email.Trim().ToLowerInvariant() });
        if (record == null) return null;

        return MapDynamicToUser(record);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, name, email, password_hash AS PasswordHash, status, 
                   last_login_at AS LastLoginAt, avatar_storage_key AS AvatarStorageKey, 
                   created_at AS CreatedAt, created_by AS CreatedBy, 
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy, 
                   row_version AS RowVersion 
            FROM users 
            ORDER BY name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var records = await conn.QueryAsync<dynamic>(sql);
        return records.Select(MapDynamicToUser).ToList();
    }

    public async Task<long> AddAsync(User user, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO users (name, email, password_hash, status, last_login_at, avatar_storage_key, created_at, created_by, updated_at, updated_by, row_version)
            VALUES (@Name, @Email, @PasswordHash, @Status, @LastLoginAt, @AvatarStorageKey, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy, @RowVersion);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, new
        {
            user.Name,
            Email = user.Email.ToLowerInvariant(),
            user.PasswordHash,
            Status = user.Status.ToString(),
            user.LastLoginAt,
            user.AvatarStorageKey,
            user.CreatedAt,
            user.CreatedBy,
            user.UpdatedAt,
            user.UpdatedBy,
            user.RowVersion
        });
        user.Id = id;
        return id;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE users 
            SET name = @Name, 
                password_hash = @PasswordHash, 
                status = @Status, 
                last_login_at = @LastLoginAt, 
                avatar_storage_key = @AvatarStorageKey, 
                updated_at = @UpdatedAt, 
                updated_by = @UpdatedBy, 
                row_version = @RowVersion
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            user.Id,
            user.Name,
            user.PasswordHash,
            Status = user.Status.ToString(),
            user.LastLoginAt,
            user.AvatarStorageKey,
            user.UpdatedAt,
            user.UpdatedBy,
            user.RowVersion
        });
    }

    public async Task<bool> ExistsByEmailAsync(string email, long? excludeUserId = null, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM users 
            WHERE email = @Email AND (@ExcludeId IS NULL OR id != @ExcludeId);";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(sql, new
        {
            Email = email.Trim().ToLowerInvariant(),
            ExcludeId = excludeUserId
        });
        return count > 0;
    }

    public async Task<IReadOnlyList<Department>> GetUserDepartmentsAsync(long userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT d.id, d.name, d.description, d.status 
            FROM departments d
            INNER JOIN user_departments ud ON ud.department_id = d.id
            WHERE ud.user_id = @UserId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var result = await conn.QueryAsync<Department>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task SetUserDepartmentsAsync(long userId, IEnumerable<long> departmentIds, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync("DELETE FROM user_departments WHERE user_id = @UserId;", new { UserId = userId }, tx);

        var list = departmentIds.Distinct().ToList();
        if (list.Count > 0)
        {
            const string insertSql = "INSERT INTO user_departments (user_id, department_id) VALUES (@UserId, @DepartmentId);";
            foreach (var deptId in list)
            {
                await conn.ExecuteAsync(insertSql, new { UserId = userId, DepartmentId = deptId }, tx);
            }
        }

        tx.Commit();
    }

    public async Task<IReadOnlyList<Role>> GetUserRolesAsync(long userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT r.id, r.name, r.description 
            FROM roles r
            INNER JOIN user_roles ur ON ur.role_id = r.id
            WHERE ur.user_id = @UserId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var result = await conn.QueryAsync<Role>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task SetUserRolesAsync(long userId, IEnumerable<long> roleIds, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync("DELETE FROM user_roles WHERE user_id = @UserId;", new { UserId = userId }, tx);

        var list = roleIds.Distinct().ToList();
        if (list.Count > 0)
        {
            const string insertSql = "INSERT INTO user_roles (user_id, role_id) VALUES (@UserId, @RoleId);";
            foreach (var rId in list)
            {
                await conn.ExecuteAsync(insertSql, new { UserId = userId, RoleId = rId }, tx);
            }
        }

        tx.Commit();
    }

    public async Task<IReadOnlyList<string>> GetUserEffectivePermissionCodesAsync(long userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT DISTINCT p.code
            FROM permissions p
            INNER JOIN role_permissions rp ON rp.permission_id = p.id
            INNER JOIN user_roles ur ON ur.role_id = rp.role_id
            WHERE ur.user_id = @UserId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var codes = await conn.QueryAsync<string>(sql, new { UserId = userId });
        return codes.ToList();
    }

    public async Task<int> CountActiveUsersWithRoleAsync(long roleId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT COUNT(DISTINCT u.id)
            FROM users u
            INNER JOIN user_roles ur ON u.id = ur.user_id
            WHERE ur.role_id = @RoleId AND u.status = 'Active';";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(sql, new { RoleId = roleId });
    }

    private static User MapDynamicToUser(dynamic record)
    {
        var statusStr = (string)record.status;
        var status = Enum.TryParse<UserStatus>(statusStr, true, out var parsedStatus)
            ? parsedStatus
            : UserStatus.Active;

        return new User
        {
            Id = (long)record.id,
            Name = (string)record.name,
            Email = (string)record.email,
            PasswordHash = (string)record.PasswordHash,
            Status = status,
            LastLoginAt = (DateTime?)record.LastLoginAt,
            AvatarStorageKey = (string?)record.AvatarStorageKey,
            CreatedAt = (DateTime)record.CreatedAt,
            CreatedBy = (long?)record.CreatedBy,
            UpdatedAt = (DateTime?)record.UpdatedAt,
            UpdatedBy = (long?)record.UpdatedBy,
            RowVersion = (long)record.RowVersion
        };
    }
}
