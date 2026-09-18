namespace TraceCore.Domain.Entities;

public class RolePermission
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }

    public RolePermission() { }

    public RolePermission(long roleId, long permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}
