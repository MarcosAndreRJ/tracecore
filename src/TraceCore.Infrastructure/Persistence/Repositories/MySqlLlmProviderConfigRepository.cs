using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlLlmProviderConfigRepository : ILlmProviderConfigRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlLlmProviderConfigRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LlmProviderConfig?> GetActiveByPurposeAsync(string purpose, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, purpose AS Purpose, provider_code AS ProviderCode, model_name AS ModelName,
                   is_active AS IsActive, created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_provider_configs
            WHERE purpose = @Purpose AND is_active = TRUE
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<LlmProviderConfig>(sql, new { Purpose = purpose });
    }

    public async Task<LlmProviderConfig?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, purpose AS Purpose, provider_code AS ProviderCode, model_name AS ModelName,
                   is_active AS IsActive, created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_provider_configs
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<LlmProviderConfig>(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<LlmProviderConfig>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, purpose AS Purpose, provider_code AS ProviderCode, model_name AS ModelName,
                   is_active AS IsActive, created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_provider_configs
            ORDER BY purpose, provider_code;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<LlmProviderConfig>(sql);
        return rows.ToList();
    }

    public async Task<LlmProviderConfig?> GetByPurposeAndProviderAsync(string purpose, string providerCode, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, purpose AS Purpose, provider_code AS ProviderCode, model_name AS ModelName,
                   is_active AS IsActive, created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_provider_configs
            WHERE purpose = @Purpose AND provider_code = @ProviderCode
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<LlmProviderConfig>(sql, new { Purpose = purpose, ProviderCode = providerCode });
    }

    public async Task<long> AddAsync(LlmProviderConfig config, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO llm_provider_configs (purpose, provider_code, model_name, is_active, created_by, created_at)
            VALUES (@Purpose, @ProviderCode, @ModelName, @IsActive, @CreatedBy, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(sql, config);
    }

    public async Task UpdateAsync(LlmProviderConfig config, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE llm_provider_configs
            SET provider_code = @ProviderCode,
                model_name = @ModelName,
                is_active = @IsActive,
                updated_by = @UpdatedBy,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, config);
    }
}