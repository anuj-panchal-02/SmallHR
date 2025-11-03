using Microsoft.AspNetCore.Http;
using SmallHR.Core.Interfaces;
using System.Threading.Tasks;

namespace SmallHR.API.Middleware;

public class ApiUsageMetricsMiddleware
{
    private readonly RequestDelegate _next;

    public ApiUsageMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider, IUsageMetricsService usageMetricsService)
    {
        await _next(context);

        // Increment metrics only for successful requests (2xx)
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            var tenantIdString = tenantProvider.TenantId;
            if (int.TryParse(tenantIdString, out var tenantId))
            {
                try
                {
                    await usageMetricsService.IncrementApiRequestCountAsync(tenantId);
                }
                catch
                {
                    // Swallow metrics exceptions to avoid impacting request flow
                }
            }
        }
    }
}


