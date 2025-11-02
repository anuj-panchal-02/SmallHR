using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallHR.Core.Entities;
using SmallHR.Core.DTOs.Subscription;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using System.Text.Json;

namespace SmallHR.Infrastructure.Services;

public class TenantLifecycleService : ITenantLifecycleService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvisioningService _provisioningService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUsageMetricsService _usageMetricsService;
    private readonly IEmailService _emailService;
    private readonly IAlertService _alertService;
    private readonly ILogger<TenantLifecycleService> _logger;

    public TenantLifecycleService(
        ApplicationDbContext context,
        ITenantProvisioningService provisioningService,
        ISubscriptionService subscriptionService,
        IUsageMetricsService usageMetricsService,
        IEmailService emailService,
        IAlertService alertService,
        ILogger<TenantLifecycleService> logger)
    {
        _context = context;
        _provisioningService = provisioningService;
        _subscriptionService = subscriptionService;
        _usageMetricsService = usageMetricsService;
        _emailService = emailService;
        _alertService = alertService;
        _logger = logger;
    }

    public async Task<(bool Success, int? TenantId, string? ErrorMessage)> SignupAsync(SignupRequest request)
    {
        try
        {
            // Check for duplicate signup (idempotency)
            if (!string.IsNullOrWhiteSpace(request.IdempotencyToken))
            {
                var existingTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.IdempotencyToken == request.IdempotencyToken);
                
                if (existingTenant != null)
                {
                    _logger.LogInformation("Duplicate signup request detected (idempotency token: {Token}), returning existing tenant {TenantId}", 
                        request.IdempotencyToken, existingTenant.Id);
                    return (true, existingTenant.Id, null);
                }
            }

            // Check for duplicate domain/name
            var duplicateTenant = await _context.Tenants
                .FirstOrDefaultAsync(t => 
                    (request.Domain != null && t.Domain == request.Domain) ||
                    t.Name.ToLower() == request.TenantName.ToLower());
            
            if (duplicateTenant != null)
            {
                return (false, null, $"Tenant with name '{request.TenantName}' or domain '{request.Domain}' already exists");
            }

            // Create tenant
            var tenant = new Tenant
            {
                Name = request.TenantName,
                Domain = request.Domain,
                Status = TenantStatus.Provisioning,
                AdminEmail = request.AdminEmail,
                AdminFirstName = request.AdminFirstName,
                AdminLastName = request.AdminLastName,
                StripeCustomerId = request.StripeCustomerId,
                PaddleCustomerId = request.PaddleCustomerId,
                IdempotencyToken = request.IdempotencyToken ?? Guid.NewGuid().ToString(),
                IsActive = true
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // Record lifecycle event
            await RecordLifecycleEventAsync(tenant.Id, TenantLifecycleEventType.Created, TenantStatus.Provisioning, TenantStatus.Provisioning, 
                "Tenant created via signup", triggeredBy: "system", 
                metadata: new Dictionary<string, object> { { "planId", request.SubscriptionPlanId ?? 0 } });

            // Start provisioning (background worker will pick it up)
            // Provisioning service will create subscription, roles, modules, admin user, etc.
            
            _logger.LogInformation("Tenant signup initiated: {TenantId} ({TenantName})", tenant.Id, tenant.Name);
            
            return (true, tenant.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tenant signup: {Message}", ex.Message);
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> CompleteProvisioningAsync(int tenantId)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null)
            {
                return (false, "Tenant not found");
            }

            if (tenant.Status != TenantStatus.Provisioning)
            {
                return (false, $"Tenant is not in Provisioning status (current: {tenant.Status})");
            }

            // Provisioning service handles the actual provisioning
            var result = await _provisioningService.ProvisionTenantAsync(
                tenantId,
                tenant.AdminEmail ?? string.Empty,
                tenant.AdminFirstName ?? string.Empty,
                tenant.AdminLastName ?? string.Empty,
                subscriptionPlanId: null,
                startTrial: false);

            if (result.Success)
            {
                tenant.Status = TenantStatus.Active;
                tenant.ProvisionedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Record lifecycle event
                await RecordLifecycleEventAsync(tenantId, TenantLifecycleEventType.ProvisioningCompleted, 
                    TenantStatus.Provisioning, TenantStatus.Active, "Provisioning completed successfully");

                // Send welcome email
                if (!string.IsNullOrWhiteSpace(tenant.AdminEmail))
                {
                    // Email is sent by provisioning service
                }

                _logger.LogInformation("Tenant provisioning completed: {TenantId} ({TenantName})", tenantId, tenant.Name);
                return (true, null);
            }
            else
            {
                tenant.Status = TenantStatus.ProvisioningFailed;
                tenant.FailureReason = result.ErrorMessage;
                await _context.SaveChangesAsync();

                await RecordLifecycleEventAsync(tenantId, TenantLifecycleEventType.ProvisioningFailed, 
                    TenantStatus.Provisioning, TenantStatus.ProvisioningFailed, result.ErrorMessage);

                return (false, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing tenant provisioning: {TenantId}, {Message}", tenantId, ex.Message);
            return (false, ex.Message);
        }
    }

    public async Task<bool> ActivateTenantAsync(int tenantId, string? externalCustomerId = null)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return false;

            var previousStatus = tenant.Status;

            // Update billing customer IDs if provided
            if (!string.IsNullOrWhiteSpace(externalCustomerId))
            {
                // Determine provider from external customer ID format or context
                // For now, default to Stripe if not set
                if (string.IsNullOrWhiteSpace(tenant.StripeCustomerId) && 
                    string.IsNullOrWhiteSpace(tenant.PaddleCustomerId))
                {
                    tenant.StripeCustomerId = externalCustomerId;
                }
            }

            // Activate tenant
            tenant.Status = TenantStatus.Active;
            tenant.ActivatedAt = DateTime.UtcNow;
            tenant.IsSubscriptionActive = true;
            
            await _context.SaveChangesAsync();

            // Record lifecycle event
            await RecordLifecycleEventAsync(tenantId, TenantLifecycleEventType.Activated, 
                previousStatus, TenantStatus.Active, "Tenant activated (billing confirmed)");

            // Grant feature access based on plan (handled by subscription)
            _logger.LogInformation("Tenant activated: {TenantId} ({TenantName})", tenantId, tenant.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating tenant: {TenantId}, {Message}", tenantId, ex.Message);
            return false;
        }
    }

    public async Task<bool> ActivateFromWebhookAsync(string externalSubscriptionId, BillingProvider provider)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.Tenant)
                .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == externalSubscriptionId);

            if (subscription == null)
            {
                _logger.LogWarning("Subscription not found for external ID: {ExternalId}", externalSubscriptionId);
                return false;
            }

            var tenant = subscription.Tenant;
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found for subscription: {SubscriptionId}", subscription.Id);
                return false;
            }

            return await ActivateTenantAsync(tenant.Id, subscription.ExternalCustomerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating tenant from webhook: {ExternalId}, {Message}", externalSubscriptionId, ex.Message);
            return false;
        }
    }

    public async Task CheckUsageLimitsAsync(int tenantId)
    {
        try
        {
            var subscription = await _subscriptionService.GetSubscriptionByTenantIdAsync(tenantId);
            if (subscription == null) return;

            var metrics = await _usageMetricsService.GetCurrentMetricsAsync(tenantId);
            var summary = await _usageMetricsService.GetUsageSummaryAsync(tenantId);

            // Check employee limit
            if (summary.EmployeeCount >= summary.EmployeeLimit && summary.EmployeeLimit > 0)
            {
                await SendUsageAlertAsync(tenantId, "employee_limit", 
                    $"Employee limit reached ({summary.EmployeeCount}/{summary.EmployeeLimit})");
                
                // Create overage alert
                try
                {
                    await _alertService.CreateOverageAlertAsync(
                        tenantId,
                        "employees",
                        summary.EmployeeLimit,
                        summary.EmployeeCount,
                        new Dictionary<string, object>
                        {
                            { "tenantName", summary.TenantName },
                            { "planLimit", summary.EmployeeLimit },
                            { "currentUsage", summary.EmployeeCount },
                            { "overage", summary.EmployeeCount - summary.EmployeeLimit }
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create employee overage alert for tenant {TenantId}", tenantId);
                }
            }
            else if (summary.EmployeeCount >= summary.EmployeeLimit * 0.9 && summary.EmployeeLimit > 0)
            {
                await SendUsageAlertAsync(tenantId, "employee_limit_warning", 
                    $"Employee limit approaching ({summary.EmployeeCount}/{summary.EmployeeLimit})");
            }

            // Check storage limit
            if (summary.StorageLimitBytes.HasValue && 
                summary.StorageBytesUsed >= summary.StorageLimitBytes.Value)
            {
                await SendUsageAlertAsync(tenantId, "storage_limit", 
                    $"Storage limit reached ({summary.StorageBytesUsed / (1024.0 * 1024.0):F2} MB / {summary.StorageLimitBytes.Value / (1024.0 * 1024.0):F2} MB)");
                
                // Create overage alert
                try
                {
                    var limitBytes = (int)summary.StorageLimitBytes.Value;
                    var usageBytes = (int)summary.StorageBytesUsed;
                    await _alertService.CreateOverageAlertAsync(
                        tenantId,
                        "storage",
                        limitBytes,
                        usageBytes,
                        new Dictionary<string, object>
                        {
                            { "tenantName", summary.TenantName },
                            { "planLimitBytes", summary.StorageLimitBytes.Value },
                            { "currentUsageBytes", summary.StorageBytesUsed },
                            { "overageBytes", summary.StorageBytesUsed - summary.StorageLimitBytes.Value },
                            { "planLimitMB", summary.StorageLimitBytes.Value / (1024.0 * 1024.0) },
                            { "currentUsageMB", summary.StorageBytesUsed / (1024.0 * 1024.0) }
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create storage overage alert for tenant {TenantId}", tenantId);
                }
            }

            // Check API rate limit
            if (summary.ApiRequestsToday >= summary.ApiLimitPerDay)
            {
                // Create overage alert
                try
                {
                    var apiLimit = (int)summary.ApiLimitPerDay;
                    var apiUsage = (int)summary.ApiRequestsToday;
                    await _alertService.CreateOverageAlertAsync(
                        tenantId,
                        "api_requests",
                        apiLimit,
                        apiUsage,
                        new Dictionary<string, object>
                        {
                            { "tenantName", summary.TenantName },
                            { "planLimitPerDay", summary.ApiLimitPerDay },
                            { "currentUsageToday", summary.ApiRequestsToday },
                            { "overage", summary.ApiRequestsToday - summary.ApiLimitPerDay }
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create API overage alert for tenant {TenantId}", tenantId);
                }
            }
            else if (summary.ApiRequestsToday >= summary.ApiLimitPerDay * 0.9)
            {
                await SendUsageAlertAsync(tenantId, "api_rate_limit_warning", 
                    $"API rate limit approaching ({summary.ApiRequestsToday}/{summary.ApiLimitPerDay})");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking usage limits for tenant: {TenantId}, {Message}", tenantId, ex.Message);
        }
    }

    public async Task CheckAllTenantsUsageAsync()
    {
        try
        {
            var activeTenants = await _context.Tenants
                .Where(t => t.Status == TenantStatus.Active && t.IsActive)
                .Select(t => t.Id)
                .ToListAsync();

            _logger.LogInformation("Checking usage limits for {Count} active tenants", activeTenants.Count);

            foreach (var tenantId in activeTenants)
            {
                await CheckUsageLimitsAsync(tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking all tenants usage: {Message}", ex.Message);
        }
    }

    public async Task SendUsageAlertAsync(int tenantId, string alertType, string message)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return;

            // Log alert
            _logger.LogWarning("Usage alert for tenant {TenantId} ({TenantName}): {AlertType} - {Message}", 
                tenantId, tenant.Name, alertType, message);

            // Send email alert to tenant admin
            if (!string.IsNullOrWhiteSpace(tenant.AdminEmail))
            {
                await _emailService.SendEmailAsync(
                    tenant.AdminEmail,
                    $"Usage Alert: {alertType}",
                    $"Dear {tenant.AdminFirstName},\n\n{message}\n\nPlease consider upgrading your plan if you need more resources.\n\nBest regards,\nSmallHR Team");
            }

            // Record lifecycle event
            await RecordLifecycleEventAsync(tenantId, TenantLifecycleEventType.PaymentFailed, 
                tenant.Status, tenant.Status, message, metadata: new Dictionary<string, object> { { "alertType", alertType } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending usage alert: {TenantId}, {Message}", tenantId, ex.Message);
        }
    }

    public async Task<bool> UpgradePlanAsync(int tenantId, int newPlanId)
    {
        return await SwitchPlanAsync(tenantId, newPlanId);
    }

    public async Task<bool> DowngradePlanAsync(int tenantId, int newPlanId)
    {
        return await SwitchPlanAsync(tenantId, newPlanId);
    }

    public async Task<bool> SwitchPlanAsync(int tenantId, int newPlanId)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return false;

            var subscription = await _subscriptionService.GetSubscriptionByTenantIdAsync(tenantId);
            if (subscription == null) return false;

            var currentPlan = await _subscriptionService.GetPlanByIdAsync(subscription.SubscriptionPlanId);
            var newPlan = await _subscriptionService.GetPlanByIdAsync(newPlanId);
            
            if (currentPlan == null || newPlan == null) return false;

            var isUpgrade = newPlan.MonthlyPrice > currentPlan.MonthlyPrice;

            // Update subscription
            var updateRequest = new UpdateSubscriptionRequest
            {
                SubscriptionPlanId = newPlanId
            };

            await _subscriptionService.UpdateSubscriptionAsync(subscription.Id, updateRequest);

            // Update tenant
            tenant.SubscriptionPlan = newPlan.Name;
            tenant.MaxEmployees = newPlan.MaxEmployees;
            await _context.SaveChangesAsync();

            // Record lifecycle event
            var eventType = isUpgrade ? TenantLifecycleEventType.Upgraded : TenantLifecycleEventType.Downgraded;
            await RecordLifecycleEventAsync(tenantId, eventType, tenant.Status, tenant.Status, 
                $"Plan switched from {currentPlan.Name} to {newPlan.Name}",
                metadata: new Dictionary<string, object> 
                { 
                    { "oldPlanId", currentPlan.Id },
                    { "newPlanId", newPlan.Id },
                    { "oldPlanName", currentPlan.Name },
                    { "newPlanName", newPlan.Name }
                });

            // Notify tenant admin
            if (!string.IsNullOrWhiteSpace(tenant.AdminEmail))
            {
                var action = isUpgrade ? "upgraded" : "downgraded";
                await _emailService.SendEmailAsync(
                    tenant.AdminEmail,
                    $"Subscription {action}",
                    $"Dear {tenant.AdminFirstName},\n\nYour subscription has been {action} to {newPlan.Name}.\n\nNew features are now available.\n\nBest regards,\nSmallHR Team");
            }

            _logger.LogInformation("Plan switched for tenant {TenantId}: {OldPlan} -> {NewPlan}", 
                tenantId, currentPlan.Name, newPlan.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching plan for tenant: {TenantId}, {Message}", tenantId, ex.Message);
            return false;
        }
    }

    public async Task<bool> SuspendTenantAsync(int tenantId, string reason, int gracePeriodDays = 30)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return false;

            var previousStatus = tenant.Status;

            tenant.Status = TenantStatus.Suspended;
            tenant.SuspendedAt = DateTime.UtcNow;
            tenant.GracePeriodEndsAt = DateTime.UtcNow.AddDays(gracePeriodDays);
            tenant.IsSubscriptionActive = false;

            await _context.SaveChangesAsync();

            // Create suspension alert
            try
            {
                var subscription = await _subscriptionService.GetSubscriptionByTenantIdAsync(tenantId);
                await _alertService.CreateSuspensionAlertAsync(
                    tenantId,
                    reason,
                    new Dictionary<string, object>
                    {
                        { "gracePeriodDays", gracePeriodDays },
                        { "gracePeriodEndsAt", tenant.GracePeriodEndsAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty },
                        { "suspendedAt", tenant.SuspendedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty },
                        { "subscriptionId", subscription?.Id }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create suspension alert for tenant {TenantId}", tenantId);
            }

            // Record lifecycle event
            await RecordLifecycleEventAsync(tenantId, TenantLifecycleEventType.Suspended, 
                previousStatus, TenantStatus.Suspended, reason, metadata: new Dictionary<string, object> 
                { { "gracePeriodDays", gracePeriodDays } });

            // Notify tenant admin
            if (!string.IsNullOrWhiteSpace(tenant.AdminEmail))
            {
                await _emailService.SendEmailAsync(
                    tenant.AdminEmail,
                    "Account Suspended",
                    $"Dear {tenant.AdminFirstName},\n\nYour account has been suspended due to: {reason}\n\nYou have a grace period until {tenant.GracePeriodEndsAt:yyyy-MM-dd} to resolve this issue.\n\nBest regards,\nSmallHR Team");
            }

            _logger.LogWarning("Tenant suspended: {TenantId} ({TenantName}), reason: {Reason}", 
                tenantId, tenant.Name, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending tenant: {TenantId}, {Message}", tenantId, ex.Message);
            return false;
        }
    }

    public async Task<bool> ResumeTenantAsync(int tenantId)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return false;

            if (tenant.Status != TenantStatus.Suspended)
            {
                return false;
            }

            var previousStatus = tenant.Status;

            tenant.Status = TenantStatus.Active;
            tenant.SuspendedAt = null;
            tenant.GracePeriodEndsAt = null;
            tenant.IsSubscriptionActive = true;

            await _context.SaveChangesAsync();

            // Record lifecycle event
            await RecordLifecycleEventAsync(tenantId, TenantLifecycleEventType.Resumed, 
                previousStatus, TenantStatus.Active, "Tenant resumed (payment recovered)");

            // Notify tenant admin
            if (!string.IsNullOrWhiteSpace(tenant.AdminEmail))
            {
                await _emailService.SendEmailAsync(
                    tenant.AdminEmail,
                    "Account Resumed",
                    $"Dear {tenant.AdminFirstName},\n\nYour account has been resumed and is now active.\n\nBest regards,\nSmallHR Team");
            }

            _logger.LogInformation("Tenant resumed: {TenantId} ({TenantName})", tenantId, tenant.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming tenant: {TenantId}, {Message}", tenantId, ex.Message);
            return false;
        }
    }

    public async Task<bool> CancelTenantAsync(int tenantId, string reason, bool scheduleDeletion = true, int retentionDays = 90)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return false;

            var previousStatus = tenant.Status;

            tenant.Status = TenantStatus.Cancelled;
            tenant.CancelledAt = DateTime.UtcNow;
            tenant.IsSubscriptionActive = false;

            if (scheduleDeletion)
            {
                tenant.Status = TenantStatus.PendingDeletion;
                tenant.ScheduledDeletionAt = DateTime.UtcNow.AddDays(retentionDays);
            }

            await _context.SaveChangesAsync();

            // Cancel subscription
            var subscription = await _subscriptionService.GetSubscriptionByTenantIdAsync(tenantId);
            if (subscription != null)
            {
                await _subscriptionService.CancelSubscriptionAsync(subscription.Id, reason);
            }

            // Record lifecycle event
            await RecordLifecycleEventAsync(tenantId, TenantLifecycleEventType.Cancelled, 
                previousStatus, tenant.Status, reason, metadata: new Dictionary<string, object> 
                { { "scheduledDeletion", scheduleDeletion }, { "retentionDays", retentionDays } });

            // Send cancellation email with export option
            if (!string.IsNullOrWhiteSpace(tenant.AdminEmail))
            {
                var exportLink = $"/api/tenants/{tenantId}/export"; // TODO: Generate secure export link
                await _emailService.SendEmailAsync(
                    tenant.AdminEmail,
                    "Account Cancelled",
                    $"Dear {tenant.AdminFirstName},\n\nYour account has been cancelled.\n\nReason: {reason}\n\n" +
                    (scheduleDeletion 
                        ? $"Your data will be retained for {retentionDays} days and can be exported at: {exportLink}\n\n" 
                        : "") +
                    $"After the retention period, your data will be permanently deleted.\n\nBest regards,\nSmallHR Team");
            }

            _logger.LogWarning("Tenant cancelled: {TenantId} ({TenantName}), reason: {Reason}", 
                tenantId, tenant.Name, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling tenant: {TenantId}, {Message}", tenantId, ex.Message);
            return false;
        }
    }

    public async Task<byte[]> ExportTenantDataAsync(int tenantId)
    {
        try
        {
            // TODO: Implement comprehensive data export
            // This should export all tenant data: employees, leave requests, attendance, etc.
            
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null)
            {
                throw new InvalidOperationException("Tenant not found");
            }

            var exportData = new
            {
                exportedAt = DateTime.UtcNow,
                tenantId = tenantId,
                tenantName = tenant.Name,
                tenantDomain = tenant.Domain,
                // Add tenant data here (employees, leave requests, attendance, etc.)
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            return System.Text.Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tenant data: {TenantId}, {Message}", tenantId, ex.Message);
            throw;
        }
    }

    public async Task<bool> SoftDeleteTenantAsync(int tenantId)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return false;

            tenant.Status = TenantStatus.PendingDeletion;
            tenant.ScheduledDeletionAt = DateTime.UtcNow.AddDays(90); // 90 days retention
            tenant.IsActive = false;

            await _context.SaveChangesAsync();

            await RecordLifecycleEventAsync(tenantId, TenantLifecycleEventType.MarkedForDeletion, 
                tenant.Status, TenantStatus.PendingDeletion, "Tenant marked for deletion");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting tenant: {TenantId}, {Message}", tenantId, ex.Message);
            return false;
        }
    }

    public async Task<bool> HardDeleteTenantAsync(int tenantId)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return false;

            // Record lifecycle event before deletion
            await RecordLifecycleEventAsync(tenantId, TenantLifecycleEventType.Deleted, 
                tenant.Status, TenantStatus.Deleted, "Tenant permanently deleted");

            // Delete tenant (cascade will handle related data)
            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Tenant permanently deleted: {TenantId} ({TenantName})", tenantId, tenant.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hard deleting tenant: {TenantId}, {Message}", tenantId, ex.Message);
            return false;
        }
    }

    public async Task ProcessPendingDeletionsAsync()
    {
        try
        {
            var tenantsToDelete = await _context.Tenants
                .Where(t => t.Status == TenantStatus.PendingDeletion &&
                           t.ScheduledDeletionAt.HasValue &&
                           t.ScheduledDeletionAt.Value <= DateTime.UtcNow)
                .ToListAsync();

            _logger.LogInformation("Processing {Count} tenants pending deletion", tenantsToDelete.Count);

            foreach (var tenant in tenantsToDelete)
            {
                await HardDeleteTenantAsync(tenant.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending deletions: {Message}", ex.Message);
        }
    }

    public async Task<List<TenantLifecycleEvent>> GetLifecycleEventsAsync(int tenantId, int limit = 100)
    {
        return await _context.TenantLifecycleEvents
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.EventDate)
            .Take(limit)
            .ToListAsync();
    }

    public async Task RecordLifecycleEventAsync(int tenantId, TenantLifecycleEventType eventType, 
        TenantStatus previousStatus, TenantStatus newStatus, string? reason = null, 
        string? triggeredBy = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var lifecycleEvent = new TenantLifecycleEvent
            {
                TenantId = tenantId,
                EventType = eventType,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                Reason = reason,
                TriggeredBy = triggeredBy ?? "system",
                EventDate = DateTime.UtcNow
            };

            if (metadata != null && metadata.Any())
            {
                lifecycleEvent.Metadata = metadata;
                lifecycleEvent.MetadataJson = JsonSerializer.Serialize(metadata);
            }

            _context.TenantLifecycleEvents.Add(lifecycleEvent);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording lifecycle event: {TenantId}, {EventType}, {Message}", 
                tenantId, eventType, ex.Message);
        }
    }

    public async Task<TenantSuspensionInfo?> GetSuspensionInfoAsync(int tenantId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return null;

        return new TenantSuspensionInfo
        {
            TenantId = tenantId,
            Status = tenant.Status,
            SuspendedAt = tenant.SuspendedAt,
            GracePeriodEndsAt = tenant.GracePeriodEndsAt,
            ScheduledDeletionAt = tenant.ScheduledDeletionAt,
            Reason = tenant.FailureReason,
            CanReactivate = tenant.Status == TenantStatus.Suspended && 
                           (tenant.GracePeriodEndsAt == null || tenant.GracePeriodEndsAt.Value > DateTime.UtcNow)
        };
    }

    public async Task<List<int>> GetTenantsPendingDeletionAsync()
    {
        return await _context.Tenants
            .Where(t => t.Status == TenantStatus.PendingDeletion)
            .Select(t => t.Id)
            .ToListAsync();
    }

    public async Task<List<int>> GetSuspendedTenantsAsync()
    {
        return await _context.Tenants
            .Where(t => t.Status == TenantStatus.Suspended)
            .Select(t => t.Id)
            .ToListAsync();
    }
}

