using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace TraceCore.Web.Security;

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Se a policy contém '.', consideramos que é uma permissão granular 'dominio.acao' (ex.: 'usuario.gerenciar')
        if (policyName.Contains('.'))
        {
            var policy = new AuthorizationPolicyBuilder();
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(policyName));
            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();
}
