using System;
using TraceCore.Domain.Enums;

namespace TraceCore.Domain.Entities;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime? LastLoginAt { get; set; }
    // TODO: ADR-P004 - Storage de anexos e avatares pendente de decisão de provider. Campo avatar_storage_key mantido para integridade.
    public string? AvatarStorageKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; }
    public long RowVersion { get; set; } = 1;

    public User() { }

    public User(string name, string email, string passwordHash, long? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome de usuário é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("E-mail é obrigatório.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Hash de senha é obrigatório.", nameof(passwordHash));

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Status = UserStatus.Active;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
        RowVersion = 1;
    }

    public void RecordLogin(DateTime loginTimeUtc)
    {
        LastLoginAt = loginTimeUtc;
    }

    public void UpdatePassword(string newPasswordHash, long? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Novo hash de senha é obrigatório.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        RowVersion++;
    }

    public void UpdateProfile(string name, string? avatarStorageKey, long? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome é obrigatório.", nameof(name));

        Name = name.Trim();
        AvatarStorageKey = avatarStorageKey;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        RowVersion++;
    }

    // BR-005: Desativar usuário não apaga autoria, comentários, casos, aprovações ou histórico.
    public void Deactivate(long? updatedBy = null)
    {
        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        RowVersion++;
    }

    public void Activate(long? updatedBy = null)
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        RowVersion++;
    }

    public void Suspend(long? updatedBy = null)
    {
        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        RowVersion++;
    }
}
