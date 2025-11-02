namespace SmallHR.Core.Entities;

/// <summary>
/// Subscription entity linked to Tenant
/// Tracks billing, subscription status, and external billing provider information
/// </summary>
public class Subscription : BaseEntity
{
    // Foreign Key to Tenant
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;
    
    // Subscription Plan Reference
    public int SubscriptionPlanId { get; set; }
    public virtual SubscriptionPlan Plan { get; set; } = null!;
    
    // Subscription Status
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    
    // Billing Period
    public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;
    
    // Dates
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public DateTime? CanceledAt { get; set; }
    
    // External Billing Provider Integration
    public string? ExternalSubscriptionId { get; set; } // Stripe subscription_id, Paddle subscription_id
    public string? ExternalCustomerId { get; set; } // Stripe customer_id, Paddle customer_id
    public BillingProvider BillingProvider { get; set; } = BillingProvider.None;
    
    // Pricing
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    
    // Auto-renewal
    public bool AutoRenew { get; set; } = true;
    
    // Cancellation
    public DateTime? CancelAtPeriodEnd { get; set; }
    public string? CancellationReason { get; set; }
    
    // Metadata
    public string? Notes { get; set; }
}

/// <summary>
/// Subscription status enumeration
/// </summary>
public enum SubscriptionStatus
{
    Active = 1,
    Trialing = 2,
    PastDue = 3,
    Canceled = 4,
    Unpaid = 5,
    Expired = 6,
    Incomplete = 7,
    IncompleteExpired = 8
}

/// <summary>
/// Billing period enumeration
/// </summary>
public enum BillingPeriod
{
    Monthly = 1,
    Quarterly = 2,
    Yearly = 3,
    Lifetime = 4
}

/// <summary>
/// Billing provider enumeration
/// </summary>
public enum BillingProvider
{
    None = 0,
    Stripe = 1,
    Paddle = 2,
    PayPal = 3,
    Custom = 99
}

