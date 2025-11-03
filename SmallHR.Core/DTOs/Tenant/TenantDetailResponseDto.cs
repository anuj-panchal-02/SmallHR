namespace SmallHR.Core.DTOs.Tenant;

public class TenantDetailResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSubscriptionActive { get; set; }
    public string? AdminEmail { get; set; }
    public string? AdminFirstName { get; set; }
    public string? AdminLastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UserCount { get; set; }
    public int EmployeeCount { get; set; }
    public TenantUsageMetricsDetailDto? UsageMetrics { get; set; }
    public List<TenantSubscriptionHistoryDto> Subscriptions { get; set; } = new();
    public List<TenantLifecycleEventDto> RecentLifecycleEvents { get; set; } = new();
}

public class TenantUsageMetricsDetailDto
{
    public long ApiRequestCount { get; set; }
    public int EmployeeCount { get; set; }
    public int UserCount { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class TenantSubscriptionHistoryDto
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TenantLifecycleEventDto
{
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? Description { get; set; }
    public object? Metadata { get; set; } // Can be Dictionary<string, object> or string
}

