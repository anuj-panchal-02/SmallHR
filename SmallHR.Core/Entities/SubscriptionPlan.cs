namespace SmallHR.Core.Entities;

/// <summary>
/// Subscription Plan entity defining available plans and their features
/// </summary>
public class SubscriptionPlan : BaseEntity
{
    public required string Name { get; set; } // Free, Basic, Pro, Enterprise
    public string? Description { get; set; }
    
    // Pricing
    public decimal MonthlyPrice { get; set; }
    public decimal? YearlyPrice { get; set; } // Nullable for plans without yearly option
    public decimal? QuarterlyPrice { get; set; }
    public string Currency { get; set; } = "USD";
    
    // External Billing Provider IDs
    public string? StripePriceId { get; set; } // Stripe Price ID for monthly
    public string? StripeYearlyPriceId { get; set; } // Stripe Price ID for yearly
    public string? PaddlePlanId { get; set; } // Paddle Plan ID
    
    // Limits
    public int MaxEmployees { get; set; }
    public int? MaxDepartments { get; set; }
    public int? MaxUsers { get; set; }
    public long? MaxStorageBytes { get; set; } // Storage limit in bytes
    
    // Features (Many-to-Many relationship)
    public virtual ICollection<SubscriptionPlanFeature> PlanFeatures { get; set; } = new List<SubscriptionPlanFeature>();
    
    // Status
    public bool IsActive { get; set; } = true;
    public bool IsVisible { get; set; } = true; // Visible in plan selection UI
    
    // Display order
    public int DisplayOrder { get; set; }
    
    // Trial
    public int? TrialDays { get; set; } // Free trial period in days
    
    // Metadata
    public string? PopularBadge { get; set; } // "Most Popular", "Best Value", etc.
    public string? Icon { get; set; } // Icon name or URL
}

