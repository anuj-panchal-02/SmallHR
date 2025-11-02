using SmallHR.Core.Entities;

namespace SmallHR.Core.DTOs.Subscription;

public class SubscriptionDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int SubscriptionPlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BillingPeriod { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public DateTime? CanceledAt { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public bool AutoRenew { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public string? ExternalCustomerId { get; set; }
    public string BillingProvider { get; set; } = string.Empty;
    public List<FeatureDto> Features { get; set; } = new();
}

public class SubscriptionPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal? YearlyPrice { get; set; }
    public decimal? QuarterlyPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int MaxEmployees { get; set; }
    public int? MaxDepartments { get; set; }
    public int? MaxUsers { get; set; }
    public long? MaxStorageBytes { get; set; }
    public int? TrialDays { get; set; }
    public bool IsActive { get; set; }
    public bool IsVisible { get; set; }
    public string? PopularBadge { get; set; }
    public string? Icon { get; set; }
    public List<FeatureDto> Features { get; set; } = new();
}

public class FeatureDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class CreateSubscriptionRequest
{
    public int TenantId { get; set; }
    public int SubscriptionPlanId { get; set; }
    public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;
    public bool StartTrial { get; set; }
}

public class UpdateSubscriptionRequest
{
    public int? SubscriptionPlanId { get; set; }
    public BillingPeriod? BillingPeriod { get; set; }
    public bool? AutoRenew { get; set; }
    public DateTime? CancelAtPeriodEnd { get; set; }
    public string? CancellationReason { get; set; }
}

public class WebhookEventDto
{
    public string EventType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty; // Stripe, Paddle
    public Dictionary<string, object> Data { get; set; } = new();
    public string? Signature { get; set; }
}

