namespace SmallHR.Core.Entities;

/// <summary>
/// Feature entity for feature-based access control
/// Defines individual features that can be enabled/disabled per plan
/// </summary>
public class Feature : BaseEntity
{
    public required string Key { get; set; } // Unique feature key (e.g., "advanced_analytics", "custom_integrations")
    public required string Name { get; set; } // Display name
    public string? Description { get; set; }
    public string? Category { get; set; } // "analytics", "integrations", "security", etc.
    
    // Feature type
    public FeatureType Type { get; set; } = FeatureType.Boolean; // Boolean, Limit, Enum
    
    // Default value (for limit-based features)
    public string? DefaultValue { get; set; }
    
    // Display
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; }
    
    // Many-to-Many: Plans that include this feature
    public virtual ICollection<SubscriptionPlanFeature> PlanFeatures { get; set; } = new List<SubscriptionPlanFeature>();
}

/// <summary>
/// Feature type enumeration
/// </summary>
public enum FeatureType
{
    Boolean = 1,    // Enabled/Disabled
    Limit = 2,     // Numeric limit (e.g., max employees)
    Enum = 3        // Enum values (e.g., "Basic", "Advanced", "Premium")
}

/// <summary>
/// Junction entity for SubscriptionPlan and Feature (Many-to-Many)
/// </summary>
public class SubscriptionPlanFeature : BaseEntity
{
    public int SubscriptionPlanId { get; set; }
    public virtual SubscriptionPlan Plan { get; set; } = null!;
    
    public int FeatureId { get; set; }
    public virtual Feature Feature { get; set; } = null!;
    
    // Feature value (for limit-based features)
    public string? Value { get; set; } // e.g., "100" for max employees, "true" for enabled
    
    // Display
    public int DisplayOrder { get; set; }
}

