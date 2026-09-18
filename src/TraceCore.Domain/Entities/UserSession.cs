using System;

namespace TraceCore.Domain.Entities;

public class UserSession
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    // 12_SEGURANCA §5/§6: Minimização de dados pessoais
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    public UserSession() { }

    public UserSession(long userId, TimeSpan validity, string? ipAddress = null, string? userAgent = null)
    {
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(validity);
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }
}
