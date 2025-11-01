using Microsoft.Extensions.Configuration;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Services;

public class ConnectionResolver : IConnectionResolver
{
    private readonly IConfiguration _configuration;
    public ConnectionResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString(string tenantId)
    {
        // Try tenant-specific connection string first: ConnectionStrings:Tenants:{tenantId}
        var key = $"ConnectionStrings:Tenants:{tenantId}";
        var tenantConn = _configuration[key];
        if (!string.IsNullOrWhiteSpace(tenantConn)) return tenantConn!;

        // Fallback to default shared database
        return _configuration.GetConnectionString("DefaultConnection")!;
    }
}


