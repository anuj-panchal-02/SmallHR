using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Tests.MultiTenancy;

public class StubTenantProvider : ITenantProvider
{
    public StubTenantProvider(string id) { TenantId = id; }
    public string TenantId { get; }
}

public class ApplicationDbContextTenantFilterTests
{
    private ApplicationDbContext CreateCtx(string tenantId, string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options, new StubTenantProvider(tenantId));
    }

    [Fact]
    public async Task GlobalFilters_Return_Only_Current_Tenant_Data()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctxAcme = CreateCtx("acme", dbName);
        ctxAcme.Modules.Add(new Module { TenantId = "acme", Name = "Dash", Path = "/dashboard", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });
        await ctxAcme.SaveChangesAsync();

        await using var ctxOther = CreateCtx("other", dbName);
        ctxOther.Modules.Add(new Module { TenantId = "other", Name = "Dash", Path = "/dashboard", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false });
        await ctxOther.SaveChangesAsync();

        await using var ctxQuery = CreateCtx("acme", dbName);
        var visible = await ctxQuery.Modules.ToListAsync();
        Assert.Single(visible);
        Assert.All(visible, m => Assert.Equal("acme", m.TenantId));
    }
}


