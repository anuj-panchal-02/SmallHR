namespace SmallHR.Core.DTOs.Tenant;

public class TenantListResponseDto
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<TenantListDto> Tenants { get; set; } = new();
}

public class TenantListDto
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
    public TenantSubscriptionDto? Subscription { get; set; }
    public int UserCount { get; set; }
    public int EmployeeCount { get; set; }
    public TenantUsageMetricsDto? UsageMetrics { get; set; }
}

public class TenantSubscriptionDto
{
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CurrentPeriodEnd { get; set; }
    public decimal Price { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
}

public class TenantUsageMetricsDto
{
    public long ApiRequestCount { get; set; }
    public DateTime LastUpdated { get; set; }
}

