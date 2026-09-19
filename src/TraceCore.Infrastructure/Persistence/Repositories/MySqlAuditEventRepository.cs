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

    public async Task<(IReadOnlyList<AuditEvent> Items, int TotalCount)> SearchAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        long? actorUserId = null,
        string? action = null,
        string? entityType = null,
        string? entityId = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        var conditions = new List<string>();
        var p = new DynamicParameters();

        if (fromDate.HasValue)
        {
            conditions.Add("occurred_at >= @FromDate");
            p.Add("FromDate", fromDate.Value);
        }
        if (toDate.HasValue)
        {
            conditions.Add("occurred_at <= @ToDate");
            p.Add("ToDate", toDate.Value);
        }
        if (actorUserId.HasValue)
        {
            conditions.Add("actor_user_id = @ActorUserId");
            p.Add("ActorUserId", actorUserId.Value);
        }
        if (!string.IsNullOrWhiteSpace(action))
        {
            conditions.Add("action = @Action");
            p.Add("Action", action.Trim());
        }
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            conditions.Add("entity_type = @EntityType");
            p.Add("EntityType", entityType.Trim());
        }
        if (!string.IsNullOrWhiteSpace(entityId))
        {
            conditions.Add("entity_id = @EntityId");
            p.Add("EntityId", entityId.Trim());
        }
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            conditions.Add("(action LIKE @Pattern OR entity_type LIKE @Pattern OR entity_id LIKE @Pattern OR correlation_id LIKE @Pattern OR user_agent_summary LIKE @Pattern OR metadata_json LIKE @Pattern OR before_json LIKE @Pattern OR after_json LIKE @Pattern)");
            p.Add("Pattern", $"%{searchTerm.Trim()}%");
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(1) FROM audit_events {whereClause};";

        var dataSql = $@"
            SELECT id, occurred_at AS OccurredAt, actor_user_id AS ActorUserId, actor_type AS ActorType, 
                   action, entity_type AS EntityType, entity_id AS EntityId, correlation_id AS CorrelationId, 
                   ip_address AS IpAddress, user_agent_summary AS UserAgentSummary, 
                   before_json AS BeforeJson, after_json AS AfterJson, metadata_json AS MetadataJson
            FROM audit_events 
            {whereClause}
            ORDER BY occurred_at DESC 
            LIMIT @Take OFFSET @Skip;";

        p.Add("Skip", Math.Max(0, skip));
        p.Add("Take", Math.Clamp(take, 1, 500));

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, p);
        var items = await conn.QueryAsync<AuditEvent>(dataSql, p);

        return (items.ToList(), totalCount);
    }
}
