using SmallHR.Core.Entities;

namespace SmallHR.Core.Interfaces;

/// <summary>
/// Service for managing complete tenant lifecycle from signup to deletion
/// </summary>
public interface ITenantLifecycleService
{
    // Signup / Provisioning
    Task<(bool Success, int? TenantId, string? ErrorMessage)> SignupAsync(SignupRequest request);
    Task<(bool Success, string? ErrorMessage)> CompleteProvisioningAsync(int tenantId);
    
    // Activation
    Task<bool> ActivateTenantAsync(int tenantId, string? externalCustomerId = null);
    Task<bool> ActivateFromWebhookAsync(string externalSubscriptionId, BillingProvider provider);
    
    // Monitoring & Alerts
    Task CheckUsageLimitsAsync(int tenantId);
    Task CheckAllTenantsUsageAsync();
    Task SendUsageAlertAsync(int tenantId, string alertType, string message);
    
    // Upgrade / Downgrade
    Task<bool> UpgradePlanAsync(int tenantId, int newPlanId);
    Task<bool> DowngradePlanAsync(int tenantId, int newPlanId);
    Task<bool> SwitchPlanAsync(int tenantId, int newPlanId);
    
    // Suspension / Cancellation
    Task<bool> SuspendTenantAsync(int tenantId, string reason, int gracePeriodDays = 30);
    Task<bool> ResumeTenantAsync(int tenantId);
    Task<bool> CancelTenantAsync(int tenantId, string reason, bool scheduleDeletion = true, int retentionDays = 90);
    
    // Data Export & Deletion
    Task<byte[]> ExportTenantDataAsync(int tenantId);
    Task<bool> SoftDeleteTenantAsync(int tenantId);
    Task<bool> HardDeleteTenantAsync(int tenantId);
    Task ProcessPendingDeletionsAsync(); // Background job
    
    // Lifecycle Events
    Task<List<TenantLifecycleEvent>> GetLifecycleEventsAsync(int tenantId, int limit = 100);
    Task RecordLifecycleEventAsync(int tenantId, TenantLifecycleEventType eventType, TenantStatus previousStatus, TenantStatus newStatus, string? reason = null, string? triggeredBy = null, Dictionary<string, object>? metadata = null);
    
    // Status Queries
    Task<TenantSuspensionInfo?> GetSuspensionInfoAsync(int tenantId);
    Task<List<int>> GetTenantsPendingDeletionAsync();
    Task<List<int>> GetSuspendedTenantsAsync();
}

public class SignupRequest
{
    public required string TenantName { get; set; }
    public string? Domain { get; set; }
    public required string AdminEmail { get; set; }
    public required string AdminFirstName { get; set; }
    public required string AdminLastName { get; set; }
    public int? SubscriptionPlanId { get; set; } // Optional, defaults to Free
    public bool StartTrial { get; set; } = false;
    public string? StripeCustomerId { get; set; }
    public string? PaddleCustomerId { get; set; }
    public string? IdempotencyToken { get; set; } // For retry safety
}

