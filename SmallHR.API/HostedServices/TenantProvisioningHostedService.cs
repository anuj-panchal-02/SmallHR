using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.API.HostedServices;

public class TenantProvisioningHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TenantProvisioningHostedService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10); // Check every 10 seconds

    public TenantProvisioningHostedService(
        IServiceProvider services,
        ILogger<TenantProvisioningHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Tenant Provisioning Hosted Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingTenantsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending tenants");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Tenant Provisioning Hosted Service stopped");
    }

    private async Task ProcessPendingTenantsAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IConnectionResolver>();
        var provisioningService = scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>();
        
        // Get connection to master/registry database
        var masterConn = resolver.GetConnectionString("default");
        var masterOpts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(masterConn)
            .Options;
        
        // Create tenant provider for master DB
        var masterTenantProvider = new MasterTenantProvider("default");
        
        await using var masterCtx = new ApplicationDbContext(masterOpts, masterTenantProvider);
        
        // Find tenants that need provisioning
        var pendingTenants = await masterCtx.Tenants
            .Where(t => t.Status == TenantStatus.Provisioning && !t.IsDeleted)
            .Take(5) // Process up to 5 at a time
            .ToListAsync(cancellationToken);

        foreach (var tenant in pendingTenants)
        {
            try
            {
                _logger.LogInformation("Processing provisioning for tenant {TenantId}: {TenantName}", tenant.Id, tenant.Name);
                
                // Get admin email from tenant record
                if (string.IsNullOrWhiteSpace(tenant.AdminEmail))
                {
                    tenant.Status = TenantStatus.ProvisioningFailed;
                    tenant.FailureReason = "Admin email is required for provisioning. Please provide adminEmail in tenant creation request.";
                    tenant.UpdatedAt = DateTime.UtcNow;
                    await masterCtx.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogWarning("Tenant {TenantId} provisioning skipped - admin email not provided", tenant.Id);
                    continue;
                }
                
                var adminEmail = tenant.AdminEmail;
                var adminFirstName = tenant.AdminFirstName ?? "Admin";
                var adminLastName = tenant.AdminLastName ?? tenant.Name ?? "User";
                
                // Provision the tenant
                var (success, errorMessage, result) = await provisioningService.ProvisionTenantAsync(
                    tenant.Id,
                    adminEmail,
                    adminFirstName,
                    adminLastName,
                    null, // subscriptionPlanId - will default to Free
                    false); // startTrial

                if (success)
                {
                    tenant.Status = TenantStatus.Active;
                    tenant.ProvisionedAt = DateTime.UtcNow;
                    tenant.FailureReason = null;
                    tenant.UpdatedAt = DateTime.UtcNow;
                    
                    _logger.LogInformation("Tenant {TenantId} provisioned successfully", tenant.Id);
                }
                else
                {
                    tenant.Status = TenantStatus.ProvisioningFailed;
                    tenant.FailureReason = errorMessage ?? "Unknown error during provisioning";
                    tenant.UpdatedAt = DateTime.UtcNow;
                    
                    _logger.LogError("Tenant {TenantId} provisioning failed: {Error}", tenant.Id, errorMessage);
                }
                
                await masterCtx.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error provisioning tenant {TenantId}: {Message}", tenant.Id, ex.Message);
                
                // Mark tenant as failed
                tenant.Status = TenantStatus.ProvisioningFailed;
                tenant.FailureReason = $"Exception during provisioning: {ex.Message}";
                tenant.UpdatedAt = DateTime.UtcNow;
                
                try
                {
                    await masterCtx.SaveChangesAsync(cancellationToken);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "Failed to save tenant {TenantId} failure status", tenant.Id);
                }
            }
        }
    }

    private class MasterTenantProvider : ITenantProvider
    {
        private readonly string _tenantId;
        
        public MasterTenantProvider(string tenantId)
        {
            _tenantId = tenantId;
        }
        
        public string TenantId => _tenantId;
    }
}

