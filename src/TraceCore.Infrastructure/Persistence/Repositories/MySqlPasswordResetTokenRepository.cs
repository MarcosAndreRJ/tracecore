using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlPasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlPasswordResetTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, user_id AS UserId, token_hash AS TokenHash, expires_at AS ExpiresAt, 
                   used_at AS UsedAt, created_at AS CreatedAt 
            FROM password_reset_tokens 
            WHERE token_hash = @TokenHash;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<PasswordResetToken>(sql, new { TokenHash = tokenHash });
    }

    public async Task<long> AddAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO password_reset_tokens (user_id, token_hash, expires_at, used_at, created_at)
            VALUES (@UserId, @TokenHash, @ExpiresAt, @UsedAt, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, token);
        token.Id = id;
        return id;
    }

    public async Task MarkAsUsedAsync(long id, CancellationToken ct = default)
    {
        const string sql = "UPDATE password_reset_tokens SET used_at = @UsedAt WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Id = id, UsedAt = DateTime.UtcNow });
    }

    public async Task InvalidateAllForUserAsync(long userId, CancellationToken ct = default)
    {
        const string sql = "UPDATE password_reset_tokens SET used_at = @UsedAt WHERE user_id = @UserId AND used_at IS NULL;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { UserId = userId, UsedAt = DateTime.UtcNow });
    }
}
