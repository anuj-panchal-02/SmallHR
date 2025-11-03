namespace SmallHR.Core.DTOs.UsageMetrics;

/// <summary>
/// Detailed dashboard metrics for a specific tenant
/// </summary>
public class TenantDashboardDto
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string SubscriptionPlan { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Current usage summary
    /// </summary>
    public TenantUsageSummaryDto CurrentUsage { get; set; } = new();
    
    /// <summary>
    /// Daily trends for the last 7 days
    /// </summary>
    public List<DailyUsagePointDto> DailyTrends { get; set; } = new();
    
    /// <summary>
    /// Weekly trends for the last 4 weeks
    /// </summary>
    public List<WeeklyUsagePointDto> WeeklyTrends { get; set; } = new();
    
    /// <summary>
    /// Monthly trends for the last 12 months
    /// </summary>
    public List<MonthlyUsagePointDto> MonthlyTrends { get; set; } = new();
    
    /// <summary>
    /// Feature usage breakdown with percentages
    /// </summary>
    public List<FeatureUsageDto> FeatureUsage { get; set; } = new();
}

/// <summary>
/// Current usage summary for tenant
/// </summary>
public class TenantUsageSummaryDto
{
    // Employee metrics
    public int EmployeeCount { get; set; }
    public int EmployeeLimit { get; set; }
    public double EmployeeUsagePercent => EmployeeLimit > 0 ? (EmployeeCount * 100.0 / EmployeeLimit) : 0;
    
    // User metrics
    public int UserCount { get; set; }
    public int? UserLimit { get; set; }
    public double UserUsagePercent => UserLimit.HasValue && UserLimit.Value > 0 ? (UserCount * 100.0 / UserLimit.Value) : 0;
    
    // Storage metrics
    public long StorageBytesUsed { get; set; }
    public long? StorageLimitBytes { get; set; }
    public double StorageUsagePercent => StorageLimitBytes.HasValue && StorageLimitBytes.Value > 0 ? (StorageBytesUsed * 100.0 / StorageLimitBytes.Value) : 0;
    
    // API request metrics
    public long ApiRequestsThisPeriod { get; set; }
    public long ApiRequestsToday { get; set; }
    public int ApiLimitPerDay { get; set; }
    public double ApiUsagePercent => ApiLimitPerDay > 0 ? (ApiRequestsToday * 100.0 / ApiLimitPerDay) : 0;
    
    // Active alerts
    public int ActiveAlertsCount { get; set; }
    
    // Period
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Daily usage data point
/// </summary>
public class DailyUsagePointDto
{
    public DateTime Date { get; set; }
    public int EmployeeCount { get; set; }
    public int UserCount { get; set; }
    public long ApiRequests { get; set; }
    public long StorageBytes { get; set; }
}

/// <summary>
/// Weekly usage data point
/// </summary>
public class WeeklyUsagePointDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public int EmployeeCount { get; set; }
    public int UserCount { get; set; }
    public long ApiRequests { get; set; }
    public long StorageBytes { get; set; }
}

/// <summary>
/// Monthly usage data point
/// </summary>
public class MonthlyUsagePointDto
{
    public DateTime MonthStart { get; set; }
    public DateTime MonthEnd { get; set; }
    public int EmployeeCount { get; set; }
    public int UserCount { get; set; }
    public long ApiRequests { get; set; }
    public long StorageBytes { get; set; }
}

/// <summary>
/// Feature usage breakdown
/// </summary>
public class FeatureUsageDto
{
    public string FeatureName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public int? Limit { get; set; }
    public double UsagePercent => Limit.HasValue && Limit.Value > 0 ? (UsageCount * 100.0 / Limit.Value) : 0;
}

