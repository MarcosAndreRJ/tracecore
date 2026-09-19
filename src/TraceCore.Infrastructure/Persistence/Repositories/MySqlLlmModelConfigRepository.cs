using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlLlmModelConfigRepository : ILlmModelConfigRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlLlmModelConfigRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LlmModelConfig?> GetActiveByPurposeAsync(string purpose, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, purpose AS Purpose, provider_id AS ProviderId, model_name AS ModelName,
                   is_active AS IsActive, created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_model_configs
            WHERE purpose = @Purpose AND is_active = TRUE
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<LlmModelConfig>(sql, new { Purpose = purpose });
    }

    public async Task<LlmModelConfig?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, purpose AS Purpose, provider_id AS ProviderId, model_name AS ModelName,
                   is_active AS IsActive, created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_model_configs
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<LlmModelConfig>(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<LlmModelConfig>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, purpose AS Purpose, provider_id AS ProviderId, model_name AS ModelName,
                   is_active AS IsActive, created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_model_configs
            ORDER BY purpose, model_name;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<LlmModelConfig>(sql);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<LlmModelConfig>> GetByProviderAsync(long providerId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, purpose AS Purpose, provider_id AS ProviderId, model_name AS ModelName,
                   is_active AS IsActive, created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_model_configs
            WHERE provider_id = @ProviderId
            ORDER BY purpose, model_name;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<LlmModelConfig>(sql, new { ProviderId = providerId });
        return rows.ToList();
    }

    public async Task<LlmModelConfig?> GetByPurposeAndProviderAsync(string purpose, long providerId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, purpose AS Purpose, provider_id AS ProviderId, model_name AS ModelName,
                   is_active AS IsActive, created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_model_configs
            WHERE purpose = @Purpose AND provider_id = @ProviderId
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<LlmModelConfig>(sql, new { Purpose = purpose, ProviderId = providerId });
    }

    public async Task<long> AddAsync(LlmModelConfig config, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO llm_model_configs (purpose, provider_id, model_name, is_active, created_by, created_at)
            VALUES (@Purpose, @ProviderId, @ModelName, @IsActive, @CreatedBy, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(sql, config);
    }

    public async Task UpdateAsync(LlmModelConfig config, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE llm_model_configs
            SET purpose = @Purpose,
                provider_id = @ProviderId,
                model_name = @ModelName,
                is_active = @IsActive,
                updated_by = @UpdatedBy,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, config);
    }
}