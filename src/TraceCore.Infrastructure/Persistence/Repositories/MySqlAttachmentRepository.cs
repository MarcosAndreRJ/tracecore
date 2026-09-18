using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlAttachmentRepository : IAttachmentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlAttachmentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(Attachment attachment, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO attachments (entity_type, entity_id, file_name, mime_type, size_bytes, sha256, storage_key, confidentiality, uploaded_by, uploaded_at)
            VALUES (@EntityType, @EntityId, @FileName, @MimeType, @SizeBytes, @Sha256, @StorageKey, @Confidentiality, @UploadedBy, @UploadedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, attachment);
        attachment.Id = id;
        return id;
    }

    public async Task<Attachment?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, entity_type AS EntityType, entity_id AS EntityId, file_name AS FileName, 
                   mime_type AS MimeType, size_bytes AS SizeBytes, sha256 AS Sha256, 
                   storage_key AS StorageKey, confidentiality AS Confidentiality, 
                   uploaded_by AS UploadedBy, uploaded_at AS UploadedAt
            FROM attachments
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Attachment>(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<Attachment>> GetByEntityAsync(string entityType, long entityId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, entity_type AS EntityType, entity_id AS EntityId, file_name AS FileName, 
                   mime_type AS MimeType, size_bytes AS SizeBytes, sha256 AS Sha256, 
                   storage_key AS StorageKey, confidentiality AS Confidentiality, 
                   uploaded_by AS UploadedBy, uploaded_at AS UploadedAt
            FROM attachments
            WHERE entity_type = @EntityType AND entity_id = @EntityId
            ORDER BY uploaded_at ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<Attachment>(sql, new { EntityType = entityType, EntityId = entityId });
        return list.ToList();
    }
}
