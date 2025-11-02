using SmallHR.Core.Entities;

namespace SmallHR.Core.Interfaces;

/// <summary>
/// Service for creating and managing system alerts
/// </summary>
public interface IAlertService : IService
{
    /// <summary>
    /// Create an alert for payment failure
    /// </summary>
    Task<Alert> CreatePaymentFailureAlertAsync(int tenantId, int? subscriptionId, string message, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Create an alert for subscription cancellation
    /// </summary>
    Task<Alert> CreateCancellationAlertAsync(int tenantId, int? subscriptionId, string reason, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Create an alert for usage overage
    /// </summary>
    Task<Alert> CreateOverageAlertAsync(int tenantId, string resource, int limit, int usage, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Create an alert for tenant suspension
    /// </summary>
    Task<Alert> CreateSuspensionAlertAsync(int tenantId, string reason, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Create an alert for system errors affecting a tenant
    /// </summary>
    Task<Alert> CreateErrorAlertAsync(int tenantId, string message, string? errorCode = null, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Check if an active alert of the same type already exists
    /// </summary>
    Task<bool> HasActiveAlertAsync(int tenantId, string alertType);
}

