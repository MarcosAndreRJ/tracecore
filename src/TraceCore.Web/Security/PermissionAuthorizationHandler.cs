using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace TraceCore.Web.Security;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var hasPermission = context.User.Claims
                .Any(c => c.Type == "permission" && string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
