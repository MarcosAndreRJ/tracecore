namespace TraceCore.Domain.Entities;

public class UserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }

    // TODO: BR-002 - Papéis podem variar por escopo/departamento em fase futura. Nesta fase, user_roles é global.
    public string? Scope { get; set; }

    public UserRole() { }

    public UserRole(long userId, long roleId, string? scope = null)
    {
        UserId = userId;
        RoleId = roleId;
        Scope = scope;
    }
}
