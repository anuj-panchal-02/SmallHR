using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.API.HostedServices;

public class ModulesWarmupHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ModulesWarmupHostedService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public ModulesWarmupHostedService(IServiceProvider services, ILogger<ModulesWarmupHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var resolver = scope.ServiceProvider.GetRequiredService<IConnectionResolver>();
                var tenantCache = scope.ServiceProvider.GetRequiredService<ITenantCache>();
                var defaultConn = resolver.GetConnectionString("default");
                var optsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(defaultConn);
                
                // Create tenant provider for default tenant
                var defaultTenantProvider = new WarmupTenantProvider("default");
                await using var defaultCtx = new ApplicationDbContext(optsBuilder.Options, defaultTenantProvider);

                var tenants = await defaultCtx.Tenants.Where(t => t.IsActive && !t.IsDeleted).Select(t => new { t.Id, t.Name }).ToListAsync(stoppingToken);
                foreach (var tenant in tenants)
                {
                    var conn = resolver.GetConnectionString(tenant.Name?.ToLowerInvariant() ?? "default");
                    var tOpts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(conn).Options;
                    var tid = tenant.Name?.ToLowerInvariant() ?? "default";
                    var tenantProvider = new WarmupTenantProvider(tid);
                    await using var tCtx = new ApplicationDbContext(tOpts, tenantProvider);
                    var modules = await tCtx.Modules.Where(m => m.TenantId == tid && m.IsActive && !m.IsDeleted)
                        .OrderBy(m => m.ParentPath).ThenBy(m => m.DisplayOrder)
                        .Select(m => new { m.Name, m.Path, m.ParentPath, m.Description, m.Icon })
                        .ToListAsync(stoppingToken);
                    await tenantCache.GetOrSetAsync(tid, "modules_tree", () => Task.FromResult(modules), TimeSpan.FromMinutes(10));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Modules warmup failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
    
    private class WarmupTenantProvider : ITenantProvider
    {
        private readonly string _tenantId;
        
        public WarmupTenantProvider(string tenantId)
        {
            _tenantId = tenantId;
        }
        
        public string TenantId => _tenantId;
    }
}


