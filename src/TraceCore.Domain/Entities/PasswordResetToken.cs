using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Extensão documentada necessária para o bloco 1.1 (Recuperação de Senha) conforme BR-101.
/// Armazena o hash do token (nunca o token em texto puro) com expiração curta.
/// </summary>
public class PasswordResetToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsValid => UsedAt == null && ExpiresAt > DateTime.UtcNow;

    public PasswordResetToken() { }

    public PasswordResetToken(long userId, string tokenHash, TimeSpan validity)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(validity);
    }

    public void MarkAsUsed()
    {
        UsedAt = DateTime.UtcNow;
    }
}
