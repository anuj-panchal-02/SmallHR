using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    public TenantResolutionMiddleware(RequestDelegate next) { _next = next; }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        // Resolve from header or host; fallback to default
        var headerId = context.Request.Headers["X-Tenant-Id"].ToString();
        var headerDomain = context.Request.Headers["X-Tenant-Domain"].ToString();
        if (!string.IsNullOrWhiteSpace(headerId))
        {
            context.Items["TenantId"] = headerId;
        }
        else if (!string.IsNullOrWhiteSpace(headerDomain))
        {
            context.Items["TenantId"] = headerDomain.ToLowerInvariant();
        }
        else
        {
            context.Items["TenantId"] = "default";
        }

        // Enforce boundary if authenticated: tenant claim must match resolved TenantId
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var claimTenant = context.User.FindFirst("tenant")?.Value;
            var resolvedTenant = context.Items["TenantId"] as string;
            if (!string.IsNullOrWhiteSpace(claimTenant) && !string.Equals(claimTenant, resolvedTenant, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Tenant mismatch.");
                return;
            }
        }

        await _next(context);
    }
}


