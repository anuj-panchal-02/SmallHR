using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SmallHR.API.Authorization;

// Supports policies in the form: Permission:/path:Action
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
        {
            // format: Permission:/path:Action
            var parts = policyName.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3)
            {
                var pagePath = parts[1];
                var actionStr = parts[2];
                if (Enum.TryParse<PermissionAction>(actionStr, ignoreCase: true, out var action))
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .AddRequirements(new PermissionRequirement(pagePath, action))
                        .RequireAuthenticatedUser()
                        .Build();
                    return Task.FromResult<AuthorizationPolicy?>(policy);
                }
            }
        }

        return FallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();
}


