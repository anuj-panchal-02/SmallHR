using Microsoft.AspNetCore.Http;
using SmallHR.API.Middleware;
using SmallHR.Core.Interfaces;

namespace SmallHR.Tests.MultiTenancy;

public class FakeTenantProvider : ITenantProvider
{
    public string TenantId { get; set; } = "default";
}

public class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task Resolves_TenantId_From_Header()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "acme";
        var provider = new FakeTenantProvider();

        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context, provider);

        Assert.Equal("acme", context.Items["TenantId"]);
    }

    [Fact]
    public async Task Enforces_Tenant_Claim_Mismatch()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "acme";
        var claimsIdentity = new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim("tenant", "other")
        }, "TestAuth");
        context.User = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);

        var provider = new FakeTenantProvider();
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context, provider);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }
}


