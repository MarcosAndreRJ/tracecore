using Microsoft.AspNetCore.Authorization;

namespace TraceCore.Web.Security;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission.Trim().ToLowerInvariant();
    }
}
