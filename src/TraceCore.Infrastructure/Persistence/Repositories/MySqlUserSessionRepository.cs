using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlUserSessionRepository : IUserSessionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlUserSessionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UserSession?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, user_id AS UserId, created_at AS CreatedAt, expires_at AS ExpiresAt, 
                   revoked_at AS RevokedAt, ip_address AS IpAddress, user_agent AS UserAgent 
            FROM user_sessions 
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<UserSession>(sql, new { Id = id });
    }

    public async Task<long> AddAsync(UserSession session, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO user_sessions (user_id, created_at, expires_at, revoked_at, ip_address, user_agent)
            VALUES (@UserId, @CreatedAt, @ExpiresAt, @RevokedAt, @IpAddress, @UserAgent);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, session);
        session.Id = id;
        return id;
    }

    public async Task RevokeAsync(long id, CancellationToken ct = default)
    {
        const string sql = "UPDATE user_sessions SET revoked_at = @RevokedAt WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Id = id, RevokedAt = DateTime.UtcNow });
    }

    public async Task RevokeAllForUserAsync(long userId, CancellationToken ct = default)
    {
        const string sql = "UPDATE user_sessions SET revoked_at = @RevokedAt WHERE user_id = @UserId AND revoked_at IS NULL;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { UserId = userId, RevokedAt = DateTime.UtcNow });
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveSessionsByUserIdAsync(long userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, user_id AS UserId, created_at AS CreatedAt, expires_at AS ExpiresAt, 
                   revoked_at AS RevokedAt, ip_address AS IpAddress, user_agent AS UserAgent 
            FROM user_sessions 
            WHERE user_id = @UserId AND revoked_at IS NULL AND expires_at > @Now;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<UserSession>(sql, new { UserId = userId, Now = DateTime.UtcNow });
        return list.ToList();
    }
}
