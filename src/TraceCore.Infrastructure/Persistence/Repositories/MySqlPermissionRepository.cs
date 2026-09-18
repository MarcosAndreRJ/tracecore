using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlPermissionRepository : IPermissionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlPermissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Permission?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT id, code, description FROM permissions WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Permission>(sql, new { Id = id });
    }

    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        const string sql = "SELECT id, code, description FROM permissions WHERE code = @Code;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Permission>(sql, new { Code = code.Trim().ToLowerInvariant() });
    }

    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT id, code, description FROM permissions ORDER BY code ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<Permission>(sql);
        return list.ToList();
    }

    public async Task<long> AddAsync(Permission permission, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO permissions (code, description) VALUES (@Code, @Description);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, new
        {
            Code = permission.Code.ToLowerInvariant(),
            permission.Description
        });
        permission.Id = id;
        return id;
    }
}
