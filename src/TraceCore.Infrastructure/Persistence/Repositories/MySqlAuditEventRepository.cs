using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlAuditEventRepository : IAuditEventRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlAuditEventRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO audit_events 
                (occurred_at, actor_user_id, actor_type, action, entity_type, entity_id, correlation_id, ip_address, user_agent_summary, before_json, after_json, metadata_json)
            VALUES 
                (@OccurredAt, @ActorUserId, @ActorType, @Action, @EntityType, @EntityId, @CorrelationId, @IpAddress, @UserAgentSummary, @BeforeJson, @AfterJson, @MetadataJson);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, auditEvent);
        auditEvent.Id = id;
        return id;
    }

    public async Task<IReadOnlyList<AuditEvent>> GetRecentAsync(int limit = 50, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, occurred_at AS OccurredAt, actor_user_id AS ActorUserId, actor_type AS ActorType, 
                   action, entity_type AS EntityType, entity_id AS EntityId, correlation_id AS CorrelationId, 
                   ip_address AS IpAddress, user_agent_summary AS UserAgentSummary, 
                   before_json AS BeforeJson, after_json AS AfterJson, metadata_json AS MetadataJson
            FROM audit_events 
            ORDER BY occurred_at DESC 
            LIMIT @Limit;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<AuditEvent>(sql, new { Limit = limit });
        return list.ToList();
    }

    public async Task<IReadOnlyList<AuditEvent>> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, occurred_at AS OccurredAt, actor_user_id AS ActorUserId, actor_type AS ActorType, 
                   action, entity_type AS EntityType, entity_id AS EntityId, correlation_id AS CorrelationId, 
                   ip_address AS IpAddress, user_agent_summary AS UserAgentSummary, 
                   before_json AS BeforeJson, after_json AS AfterJson, metadata_json AS MetadataJson
            FROM audit_events 
            WHERE entity_type = @EntityType AND entity_id = @EntityId
            ORDER BY occurred_at DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<AuditEvent>(sql, new { EntityType = entityType, EntityId = entityId });
        return list.ToList();
    }
}
