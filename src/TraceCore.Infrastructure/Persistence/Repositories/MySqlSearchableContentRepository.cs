using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlSearchableContentRepository : ISearchableContentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlSearchableContentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> UpsertAsync(SearchableContentEntry entry, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string findSql = @"
            SELECT id 
            FROM searchable_content_entries 
            WHERE source_type = @SourceType 
              AND source_id = @SourceId 
              AND ((source_version_id IS NULL AND @SourceVersionId IS NULL) OR source_version_id = @SourceVersionId)
            LIMIT 1;";

        var existingId = await conn.ExecuteScalarAsync<long?>(findSql, new
        {
            entry.SourceType,
            entry.SourceId,
            entry.SourceVersionId
        });

        if (existingId.HasValue && existingId.Value > 0)
        {
            entry.Id = existingId.Value;
            entry.UpdatedAt = DateTime.UtcNow;

            // Se o conteúdo mudou (hash diferente), o embedding antigo fica stale:
            // limpa os campos para a indexação idempotente gerar um vetor novo (Fase 13).
            // Se não mudou, preserva o embedding existente.
            using (var findConn = await _connectionFactory.CreateConnectionAsync(ct))
            {
                var existing = await findConn.QuerySingleOrDefaultAsync<SearchableContentEntry>(
                    "SELECT content_hash AS ContentHash, embedding_vector AS EmbeddingVectorJson, " +
                    "embedding_model AS EmbeddingModel, embedding_generated_at AS EmbeddingGeneratedAt " +
                    "FROM searchable_content_entries WHERE id = @Id;",
                    new { Id = entry.Id });

                bool contentChanged = existing == null ||
                    !string.Equals(existing.ContentHash, entry.ContentHash, StringComparison.OrdinalIgnoreCase);

                if (contentChanged)
                {
                    entry.EmbeddingVectorJson = null;
                    entry.EmbeddingModel = null;
                    entry.EmbeddingGeneratedAt = null;
                }
                else if (existing != null)
                {
                    entry.EmbeddingVectorJson = existing.EmbeddingVectorJson;
                    entry.EmbeddingModel = existing.EmbeddingModel;
                    entry.EmbeddingGeneratedAt = existing.EmbeddingGeneratedAt;
                }
            }

            const string updateSql = @"
                UPDATE searchable_content_entries
                SET title = @Title,
                    normalized_content = @NormalizedContent,
                    content_hash = @ContentHash,
                    validation_status = @ValidationStatus,
                    quality_status = @QualityStatus,
                    visibility = @Visibility,
                    client_id = @ClientId,
                    product_id = @ProductId,
                    component_ids_json = @ComponentIdsJson,
                    metadata_json = @MetadataJson,
                    updated_at = @UpdatedAt,
                    source_updated_at = @SourceUpdatedAt,
                    embedding_vector = @EmbeddingVectorJson,
                    embedding_model = @EmbeddingModel,
                    embedding_generated_at = @EmbeddingGeneratedAt
                WHERE id = @Id;";

            await conn.ExecuteAsync(updateSql, entry);
            return entry.Id;
        }
        else
        {
            entry.CreatedAt = DateTime.UtcNow;
            entry.UpdatedAt = DateTime.UtcNow;

            const string insertSql = @"
                INSERT INTO searchable_content_entries (
                    source_type, source_id, source_version_id, title, normalized_content,
                    content_hash, validation_status, quality_status, visibility, client_id,
                    product_id, component_ids_json, metadata_json, created_at, updated_at,
                    source_updated_at, indexed_at, embedding_version
                ) VALUES (
                    @SourceType, @SourceId, @SourceVersionId, @Title, @NormalizedContent,
                    @ContentHash, @ValidationStatus, @QualityStatus, @Visibility, @ClientId,
                    @ProductId, @ComponentIdsJson, @MetadataJson, @CreatedAt, @UpdatedAt,
                    @SourceUpdatedAt, @IndexedAt, @EmbeddingVersion
                );
                SELECT LAST_INSERT_ID();";

            var newId = await conn.ExecuteScalarAsync<long>(insertSql, entry);
            entry.Id = newId;
            return newId;
        }
    }

    public async Task<SearchableContentEntry?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, source_type AS SourceType, source_id AS SourceId, source_version_id AS SourceVersionId,
                   title AS Title, normalized_content AS NormalizedContent, content_hash AS ContentHash,
                   validation_status AS ValidationStatus, quality_status AS QualityStatus, visibility AS Visibility,
                   client_id AS ClientId, product_id AS ProductId, component_ids_json AS ComponentIdsJson,
                   metadata_json AS MetadataJson, created_at AS CreatedAt, updated_at AS UpdatedAt,
                   source_updated_at AS SourceUpdatedAt, indexed_at AS IndexedAt, embedding_version AS EmbeddingVersion,
                   embedding_vector AS EmbeddingVectorJson, embedding_model AS EmbeddingModel,
                   embedding_generated_at AS EmbeddingGeneratedAt
            FROM searchable_content_entries
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<SearchableContentEntry>(sql, new { Id = id });
    }

    public async Task<SearchableContentEntry?> GetBySourceAsync(string sourceType, long sourceId, long? sourceVersionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, source_type AS SourceType, source_id AS SourceId, source_version_id AS SourceVersionId,
                   title AS Title, normalized_content AS NormalizedContent, content_hash AS ContentHash,
                   validation_status AS ValidationStatus, quality_status AS QualityStatus, visibility AS Visibility,
                   client_id AS ClientId, product_id AS ProductId, component_ids_json AS ComponentIdsJson,
                   metadata_json AS MetadataJson, created_at AS CreatedAt, updated_at AS UpdatedAt,
                   source_updated_at AS SourceUpdatedAt, indexed_at AS IndexedAt, embedding_version AS EmbeddingVersion,
                   embedding_vector AS EmbeddingVectorJson, embedding_model AS EmbeddingModel,
                   embedding_generated_at AS EmbeddingGeneratedAt
            FROM searchable_content_entries
            WHERE source_type = @SourceType 
              AND source_id = @SourceId 
              AND ((source_version_id IS NULL AND @SourceVersionId IS NULL) OR source_version_id = @SourceVersionId)
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<SearchableContentEntry>(sql, new
        {
            SourceType = sourceType,
            SourceId = sourceId,
            SourceVersionId = sourceVersionId
        });
    }

    public async Task<(IReadOnlyList<SearchableContentEntry> Items, int TotalCount)> SearchAsync(
        string? sourceType = null,
        string? validationStatus = null,
        string? qualityStatus = null,
        string? visibility = null,
        long? clientId = null,
        long? productId = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        var conditions = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            conditions.Add("source_type = @SourceType");
            p.Add("SourceType", sourceType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(validationStatus))
        {
            conditions.Add("validation_status = @ValidationStatus");
            p.Add("ValidationStatus", validationStatus.Trim());
        }

        if (!string.IsNullOrWhiteSpace(qualityStatus))
        {
            conditions.Add("quality_status = @QualityStatus");
            p.Add("QualityStatus", qualityStatus.Trim());
        }

        if (!string.IsNullOrWhiteSpace(visibility))
        {
            conditions.Add("visibility = @Visibility");
            p.Add("Visibility", visibility.Trim());
        }

        if (clientId.HasValue)
        {
            conditions.Add("client_id = @ClientId");
            p.Add("ClientId", clientId.Value);
        }

        if (productId.HasValue)
        {
            conditions.Add("product_id = @ProductId");
            p.Add("ProductId", productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            conditions.Add("(title LIKE @Pattern OR normalized_content LIKE @Pattern OR metadata_json LIKE @Pattern)");
            p.Add("Pattern", $"%{searchTerm.Trim()}%");
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(1) FROM searchable_content_entries {whereClause};";

        var dataSql = $@"
            SELECT id, source_type AS SourceType, source_id AS SourceId, source_version_id AS SourceVersionId,
                   title AS Title, normalized_content AS NormalizedContent, content_hash AS ContentHash,
                   validation_status AS ValidationStatus, quality_status AS QualityStatus, visibility AS Visibility,
                   client_id AS ClientId, product_id AS ProductId, component_ids_json AS ComponentIdsJson,
                   metadata_json AS MetadataJson, created_at AS CreatedAt, updated_at AS UpdatedAt,
                   source_updated_at AS SourceUpdatedAt, indexed_at AS IndexedAt, embedding_version AS EmbeddingVersion,
                   embedding_vector AS EmbeddingVectorJson, embedding_model AS EmbeddingModel,
                   embedding_generated_at AS EmbeddingGeneratedAt
            FROM searchable_content_entries
            {whereClause}
            ORDER BY updated_at DESC
            LIMIT @Take OFFSET @Skip;";

        p.Add("Skip", Math.Max(0, skip));
        p.Add("Take", Math.Clamp(take, 1, 500));

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, p);
        var items = await conn.QueryAsync<SearchableContentEntry>(dataSql, p);

        return (items.ToList(), totalCount);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetCountByQualityStatusAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT quality_status, COUNT(1) AS Total
            FROM searchable_content_entries
            GROUP BY quality_status;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<(string QualityStatus, int Total)>(sql);
        return rows.ToDictionary(r => r.QualityStatus, r => r.Total, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetCountBySourceTypeAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT source_type, COUNT(1) AS Total
            FROM searchable_content_entries
            GROUP BY source_type;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<(string SourceType, int Total)>(sql);
        return rows.ToDictionary(r => r.SourceType, r => r.Total, StringComparer.OrdinalIgnoreCase);
    }

    // ---- Fase 13 (M12): recuperação assistida por IA ----

    public async Task<IReadOnlyList<SearchableContentEntry>> GetRagCandidatesAsync(
        IReadOnlyList<string> visibilities,
        string? searchTerm,
        int limit,
        CancellationToken ct = default)
    {
        var conditions = new List<string>
        {
            "validation_status IN ('Validated', 'PendingValidation')",
            "quality_status IN ('Complete', 'Validated')",
            "embedding_vector IS NOT NULL"
        };
        var p = new DynamicParameters();

        if (visibilities != null && visibilities.Count > 0)
        {
            var placeholders = new List<string>();
            for (int i = 0; i < visibilities.Count; i++)
            {
                var param = $"Vis{i}";
                placeholders.Add($"@{param}");
                p.Add(param, visibilities[i]);
            }
            conditions.Add($"visibility IN ({string.Join(", ", placeholders)})");
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            conditions.Add("(title LIKE @Pattern OR normalized_content LIKE @Pattern)");
            p.Add("Pattern", $"%{searchTerm.Trim()}%");
        }

        var sql = $@"
            SELECT id, source_type AS SourceType, source_id AS SourceId, source_version_id AS SourceVersionId,
                   title AS Title, normalized_content AS NormalizedContent, content_hash AS ContentHash,
                   validation_status AS ValidationStatus, quality_status AS QualityStatus, visibility AS Visibility,
                   client_id AS ClientId, product_id AS ProductId, component_ids_json AS ComponentIdsJson,
                   metadata_json AS MetadataJson, created_at AS CreatedAt, updated_at AS UpdatedAt,
                   source_updated_at AS SourceUpdatedAt, indexed_at AS IndexedAt, embedding_version AS EmbeddingVersion,
                   embedding_vector AS EmbeddingVectorJson, embedding_model AS EmbeddingModel,
                   embedding_generated_at AS EmbeddingGeneratedAt
            FROM searchable_content_entries
            WHERE {string.Join(" AND ", conditions)}
            ORDER BY updated_at DESC
            LIMIT @Limit;";

        p.Add("Limit", Math.Clamp(limit, 1, 500));

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<SearchableContentEntry>(sql, p);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SearchableContentEntry>> GetPendingEmbeddingAsync(
        string embeddingModel,
        int limit,
        CancellationToken ct = default)
    {
        // Indexação idempotente: só entram entradas sem vetor para o modelo atual de embedding
        // (ou gerado com outro modelo), visibilidade Public/Internal (nunca envia conteúdo
        // Confidential/Restricted para a API externa — decisão conservadora de BR-102).
        const string sql = @"
            SELECT id, source_type AS SourceType, source_id AS SourceId, source_version_id AS SourceVersionId,
                   title AS Title, normalized_content AS NormalizedContent, content_hash AS ContentHash,
                   validation_status AS ValidationStatus, quality_status AS QualityStatus, visibility AS Visibility,
                   client_id AS ClientId, product_id AS ProductId, component_ids_json AS ComponentIdsJson,
                   metadata_json AS MetadataJson, created_at AS CreatedAt, updated_at AS UpdatedAt,
                   source_updated_at AS SourceUpdatedAt, indexed_at AS IndexedAt, embedding_version AS EmbeddingVersion,
                   embedding_vector AS EmbeddingVectorJson, embedding_model AS EmbeddingModel,
                   embedding_generated_at AS EmbeddingGeneratedAt
            FROM searchable_content_entries
            WHERE validation_status = 'Validated'
              AND quality_status IN ('Complete', 'Validated')
              AND visibility IN ('Public', 'Internal')
              AND (embedding_vector IS NULL OR embedding_model IS NULL OR embedding_model <> @Model)
            ORDER BY updated_at ASC
            LIMIT @Limit;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<SearchableContentEntry>(sql, new { Model = embeddingModel, Limit = Math.Clamp(limit, 1, 1000) });
        return rows.ToList();
    }

    public async Task UpdateEmbeddingAsync(
        long id,
        string? embeddingVectorJson,
        string embeddingModel,
        DateTime? generatedAt,
        CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE searchable_content_entries
            SET embedding_vector = @EmbeddingVectorJson,
                embedding_model = @EmbeddingModel,
                embedding_generated_at = @GeneratedAt,
                indexed_at = @GeneratedAt,
                updated_at = @GeneratedAt
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Id = id, EmbeddingVectorJson = embeddingVectorJson, EmbeddingModel = embeddingModel, GeneratedAt = generatedAt });
    }
}
