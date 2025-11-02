namespace SmallHR.Core.Entities;

public class Tenant : BaseEntity
{
    public required string Name { get; set; }
    public string? Domain { get; set; }
    public bool IsActive { get; set; } = true;
    
    // SaaS Subscription Fields
    public string SubscriptionPlan { get; set; } = "Free"; // Free, Basic, Pro, Enterprise
    public int MaxEmployees { get; set; } = 10; // Maximum employees allowed
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public bool IsSubscriptionActive { get; set; } = true;
    
    // Lifecycle Management Fields
    public TenantStatus Status { get; set; } = TenantStatus.Provisioning;
    public DateTime? ProvisionedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ScheduledDeletionAt { get; set; }
    public DateTime? GracePeriodEndsAt { get; set; }
    
    // Billing Provider Integration
    public string? StripeCustomerId { get; set; }
    public string? PaddleCustomerId { get; set; }
    
    // Provisioning Fields
    public string? FailureReason { get; set; }
    public string? IdempotencyToken { get; set; }
    public string? AdminEmail { get; set; } // Admin email for tenant admin user creation
    public string? AdminFirstName { get; set; } // Admin first name
    public string? AdminLastName { get; set; } // Admin last name
    
    // Navigation properties
    public virtual ICollection<TenantLifecycleEvent> LifecycleEvents { get; set; } = new List<TenantLifecycleEvent>();
}


