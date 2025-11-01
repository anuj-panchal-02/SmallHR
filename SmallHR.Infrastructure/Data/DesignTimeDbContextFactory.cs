using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var envConn = Environment.GetEnvironmentVariable("EF_CONNECTION");
        var connectionString = string.IsNullOrWhiteSpace(envConn)
            ? "Server=(localdb)\\mssqllocaldb;Database=SmallHRDb;Trusted_Connection=true;MultipleActiveResultSets=true"
            : envConn!;

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        
        // Design-time context requires a tenant provider (use default tenant)
        var mockTenantProvider = new DesignTimeTenantProvider();
        return new ApplicationDbContext(optionsBuilder.Options, mockTenantProvider);
    }
    
    private class DesignTimeTenantProvider : ITenantProvider
    {
        public string TenantId => "default";
    }
}


