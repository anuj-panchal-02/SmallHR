using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallHR.API.Controllers;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Tests.Controllers;

public class TenantsControllerTests
{
    private class MockTenantProvider : ITenantProvider
    {
        public MockTenantProvider(string tenantId) { TenantId = tenantId; }
        public string TenantId { get; }
    }

    private ApplicationDbContext CreateCtx(string tenantId = "default")
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options, new MockTenantProvider(tenantId));
    }

    private ILogger<TenantsController> CreateLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        return loggerFactory.CreateLogger<TenantsController>();
    }

    [Fact]
    public async Task Create_Tenant_Succeeds_And_Rejects_Duplicate_Domain()
    {
        await using var ctx = CreateCtx();
        var logger = CreateLogger();
        var controller = new TenantsController(ctx, logger);

        var res1 = await controller.Create(new TenantsController.CreateTenantRequest("Acme", "acme.local", "admin@acme.com", null, null, null, true, "Free", 10));
        Assert.IsType<CreatedAtActionResult>(res1);

        var res2 = await controller.Create(new TenantsController.CreateTenantRequest("Other", "acme.local", "admin@other.com", null, null, null, true, "Free", 10));
        Assert.IsType<ConflictObjectResult>(res2);
    }
}


