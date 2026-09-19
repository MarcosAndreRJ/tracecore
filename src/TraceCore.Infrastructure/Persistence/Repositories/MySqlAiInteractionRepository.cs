using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlAiInteractionRepository : IAiInteractionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlAiInteractionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddInteractionAsync(AiInteraction interaction, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO ai_interactions (user_id, query_text, response_text, provider_code, model_name, tokens_used, latency_ms, created_at)
            VALUES (@UserId, @QueryText, @ResponseText, @ProviderCode, @ModelName, @TokensUsed, @LatencyMs, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(sql, interaction);
    }

    public async Task AddSourcesAsync(IEnumerable<AiSource> sources, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO ai_sources (ai_interaction_id, searchable_content_entry_id, rank, similarity_score, created_at)
            VALUES (@AiInteractionId, @SearchableContentEntryId, @Rank, @SimilarityScore, @CreatedAt);";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, sources);
    }

    public async Task<long> AddFeedbackAsync(AiInteractionFeedback feedback, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO ai_interaction_feedback (ai_interaction_id, user_id, useful, comment, created_at)
            VALUES (@AiInteractionId, @UserId, @Useful, @Comment, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(sql, feedback);
    }

    public async Task<AiInteraction?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, user_id AS UserId, query_text AS QueryText, response_text AS ResponseText,
                   provider_code AS ProviderCode, model_name AS ModelName, tokens_used AS TokensUsed,
                   latency_ms AS LatencyMs, created_at AS CreatedAt
            FROM ai_interactions
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<AiInteraction>(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<AiSource>> GetSourcesByInteractionIdAsync(long interactionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, ai_interaction_id AS AiInteractionId, searchable_content_entry_id AS SearchableContentEntryId,
                   rank AS Rank, similarity_score AS SimilarityScore, created_at AS CreatedAt
            FROM ai_sources
            WHERE ai_interaction_id = @AiInteractionId
            ORDER BY rank;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<AiSource>(sql, new { AiInteractionId = interactionId });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<AiInteraction>> GetInteractionsByUserAsync(long userId, int take = 20, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, user_id AS UserId, query_text AS QueryText, response_text AS ResponseText,
                   provider_code AS ProviderCode, model_name AS ModelName, tokens_used AS TokensUsed,
                   latency_ms AS LatencyMs, created_at AS CreatedAt
            FROM ai_interactions
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            LIMIT @Take;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<AiInteraction>(sql, new { UserId = userId, Take = take });
        return rows.ToList();
    }
}