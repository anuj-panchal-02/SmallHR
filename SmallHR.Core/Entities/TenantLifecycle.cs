namespace SmallHR.Core.Entities;

/// <summary>
/// Enhanced tenant status for complete lifecycle management
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// Tenant is being provisioned (initial setup)
    /// </summary>
    Provisioning = 0,
    
    /// <summary>
    /// Tenant is active and operational
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Tenant provisioning failed
    /// </summary>
    ProvisioningFailed = 2,
    
    /// <summary>
    /// Tenant is suspended (payment failure, etc.)
    /// </summary>
    Suspended = 3,
    
    /// <summary>
    /// Tenant has been cancelled
    /// </summary>
    Cancelled = 4,
    
    /// <summary>
    /// Tenant is marked for deletion (soft-delete with retention period)
    /// </summary>
    PendingDeletion = 5,
    
    /// <summary>
    /// Tenant has been deleted (hard delete after retention)
    /// </summary>
    Deleted = 6
}

/// <summary>
/// Tenant lifecycle events for tracking tenant state changes
/// </summary>
public class TenantLifecycleEvent : BaseEntity
{
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;
    
    public TenantLifecycleEventType EventType { get; set; }
    public TenantStatus PreviousStatus { get; set; }
    public TenantStatus NewStatus { get; set; }
    
    public string? Reason { get; set; }
    public string? TriggeredBy { get; set; } // User ID or system
    
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? MetadataJson { get; set; } // JSON serialized for EF Core
    
    public DateTime EventDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of lifecycle events
/// </summary>
public enum TenantLifecycleEventType
{
    Created = 0,
    ProvisioningStarted = 1,
    ProvisioningCompleted = 2,
    ProvisioningFailed = 3,
    Activated = 4,
    Suspended = 5,
    Resumed = 6,
    Upgraded = 7,
    Downgraded = 8,
    Cancelled = 9,
    MarkedForDeletion = 10,
    Deleted = 11,
    PaymentFailed = 12,
    PaymentRecovered = 13,
    GracePeriodStarted = 14,
    GracePeriodExpired = 15
}

/// <summary>
/// Tenant suspension/cancellation details
/// </summary>
public class TenantSuspensionInfo
{
    public int TenantId { get; set; }
    public TenantStatus Status { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public DateTime? GracePeriodEndsAt { get; set; }
    public DateTime? ScheduledDeletionAt { get; set; }
    public string? Reason { get; set; }
    public bool CanReactivate { get; set; } = true;
}

