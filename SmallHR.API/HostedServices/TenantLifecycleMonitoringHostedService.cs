using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.HostedServices;

/// <summary>
/// Background service for monitoring tenant lifecycle
/// - Checks usage limits for all active tenants
/// - Processes pending deletions
/// - Checks grace periods
/// </summary>
public class TenantLifecycleMonitoringHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantLifecycleMonitoringHostedService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

    public TenantLifecycleMonitoringHostedService(
        IServiceProvider serviceProvider,
        ILogger<TenantLifecycleMonitoringHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Tenant Lifecycle Monitoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var lifecycleService = scope.ServiceProvider.GetRequiredService<ITenantLifecycleService>();

                // Check usage limits for all tenants
                _logger.LogDebug("Checking usage limits for all tenants");
                await lifecycleService.CheckAllTenantsUsageAsync();

                // Process pending deletions
                _logger.LogDebug("Processing pending deletions");
                await lifecycleService.ProcessPendingDeletionsAsync();

                // Check grace periods for suspended tenants
                await CheckGracePeriodsAsync(scope.ServiceProvider, lifecycleService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in tenant lifecycle monitoring: {Message}", ex.Message);
            }

            // Wait for next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Tenant Lifecycle Monitoring Service stopped");
    }

    private async Task CheckGracePeriodsAsync(IServiceProvider serviceProvider, ITenantLifecycleService lifecycleService)
    {
        try
        {
            var suspendedTenants = await lifecycleService.GetSuspendedTenantsAsync();
            
            foreach (var tenantId in suspendedTenants)
            {
                var suspensionInfo = await lifecycleService.GetSuspensionInfoAsync(tenantId);
                if (suspensionInfo == null) continue;

                // Check if grace period has expired
                if (suspensionInfo.GracePeriodEndsAt.HasValue && 
                    suspensionInfo.GracePeriodEndsAt.Value <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Grace period expired for tenant {TenantId}, cancelling", tenantId);
                    await lifecycleService.CancelTenantAsync(tenantId, 
                        "Grace period expired - payment not recovered");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking grace periods: {Message}", ex.Message);
        }
    }
}

