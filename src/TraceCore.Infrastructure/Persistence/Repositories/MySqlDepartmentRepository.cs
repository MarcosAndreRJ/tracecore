using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlDepartmentRepository : IDepartmentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlDepartmentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Department?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT id, name, description, status FROM departments WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Department>(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT id, name, description, status FROM departments ORDER BY name ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<Department>(sql);
        return list.ToList();
    }

    public async Task<long> AddAsync(Department department, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO departments (name, description, status) 
            VALUES (@Name, @Description, @Status);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, department);
        department.Id = id;
        return id;
    }

    public async Task UpdateAsync(Department department, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE departments 
            SET name = @Name, description = @Description, status = @Status 
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, department);
    }

    public async Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM departments 
            WHERE name = @Name AND (@ExcludeId IS NULL OR id != @ExcludeId);";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Name = name.Trim(), ExcludeId = excludeId });
        return count > 0;
    }
}
