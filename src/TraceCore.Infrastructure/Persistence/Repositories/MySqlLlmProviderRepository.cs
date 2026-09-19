using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlLlmProviderRepository : ILlmProviderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlLlmProviderRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LlmProvider?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, name AS Name, code AS Code, protocol AS Protocol, base_url AS BaseUrl,
                   authentication_type AS AuthenticationType,
                   has_generation_capability AS HasGenerationCapability,
                   has_embedding_capability AS HasEmbeddingCapability,
                   status AS Status,
                   created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_providers
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<LlmProvider>(sql, new { Id = id });
    }

    public async Task<LlmProvider?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, name AS Name, code AS Code, protocol AS Protocol, base_url AS BaseUrl,
                   authentication_type AS AuthenticationType,
                   has_generation_capability AS HasGenerationCapability,
                   has_embedding_capability AS HasEmbeddingCapability,
                   status AS Status,
                   created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_providers
            WHERE code = @Code;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<LlmProvider>(sql, new { Code = code?.Trim().ToLowerInvariant() });
    }

    public async Task<IReadOnlyList<LlmProvider>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, name AS Name, code AS Code, protocol AS Protocol, base_url AS BaseUrl,
                   authentication_type AS AuthenticationType,
                   has_generation_capability AS HasGenerationCapability,
                   has_embedding_capability AS HasEmbeddingCapability,
                   status AS Status,
                   created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_providers
            ORDER BY name;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<LlmProvider>(sql);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<LlmProvider>> GetByProtocolAsync(string protocol, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, name AS Name, code AS Code, protocol AS Protocol, base_url AS BaseUrl,
                   authentication_type AS AuthenticationType,
                   has_generation_capability AS HasGenerationCapability,
                   has_embedding_capability AS HasEmbeddingCapability,
                   status AS Status,
                   created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_providers
            WHERE protocol = @Protocol
            ORDER BY name;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<LlmProvider>(sql, new { Protocol = protocol });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<LlmProvider>> GetByCapabilityAsync(string capability, CancellationToken ct = default)
    {
        var column = capability.Equals("Generation", StringComparison.OrdinalIgnoreCase)
            ? "has_generation_capability"
            : "has_embedding_capability";

        var sql = $@"
            SELECT id, name AS Name, code AS Code, protocol AS Protocol, base_url AS BaseUrl,
                   authentication_type AS AuthenticationType,
                   has_generation_capability AS HasGenerationCapability,
                   has_embedding_capability AS HasEmbeddingCapability,
                   status AS Status,
                   created_by AS CreatedBy, created_at AS CreatedAt,
                   updated_by AS UpdatedBy, updated_at AS UpdatedAt
            FROM llm_providers
            WHERE {column} = TRUE
            ORDER BY name;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<LlmProvider>(sql);
        return rows.ToList();
    }

    public async Task<long> AddAsync(LlmProvider provider, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO llm_providers (name, code, protocol, base_url, authentication_type,
                                       has_generation_capability, has_embedding_capability,
                                       status, created_by, created_at)
            VALUES (@Name, @Code, @Protocol, @BaseUrl, @AuthenticationType,
                    @HasGenerationCapability, @HasEmbeddingCapability,
                    @Status, @CreatedBy, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(sql, provider);
    }

    public async Task UpdateAsync(LlmProvider provider, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE llm_providers
            SET name = @Name,
                protocol = @Protocol,
                base_url = @BaseUrl,
                authentication_type = @AuthenticationType,
                has_generation_capability = @HasGenerationCapability,
                has_embedding_capability = @HasEmbeddingCapability,
                status = @Status,
                updated_by = @UpdatedBy,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, provider);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM llm_providers WHERE code = @Code;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Code = code?.Trim().ToLowerInvariant() });
        return count > 0;
    }
}