using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SmallHR.API.Middleware;

/// <summary>
/// Middleware to temporarily disable tenant query filters for SuperAdmin on specific admin endpoints
/// This allows SuperAdmin to access all tenants' data only on admin endpoints
/// </summary>
public class SuperAdminQueryFilterBypassMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SuperAdminQueryFilterBypassMiddleware> _logger;

    // Admin endpoints that require tenant query filter bypass
    private static readonly string[] AdminEndpoints = new[]
    {
        "/api/usermanagement",
        "/api/admin",
        "/api/tenants",
        "/api/subscriptions/plans", // Admin-only subscription plan management
        "/api/billing/webhooks", // Billing webhooks
    };

    public SuperAdminQueryFilterBypassMiddleware(
        RequestDelegate next,
        ILogger<SuperAdminQueryFilterBypassMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if user is SuperAdmin
        var isSuperAdmin = context.User?.IsInRole("SuperAdmin") == true;

        if (isSuperAdmin)
        {
            // Check if this is an admin endpoint
            var isAdminEndpoint = AdminEndpoints.Any(endpoint =>
                context.Request.Path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase));

            if (isAdminEndpoint)
            {
                // Temporarily disable tenant query filters for this request
                context.Items["BypassTenantQueryFilters"] = true;
                _logger.LogDebug(
                    "SuperAdmin query filter bypass enabled for endpoint: {Endpoint}",
                    context.Request.Path);
            }
        }

        await _next(context);
    }
}

