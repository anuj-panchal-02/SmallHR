namespace SmallHR.Core.DTOs.UsageMetrics;

/// <summary>
/// Dashboard overview DTO containing aggregated metrics across all tenants
/// </summary>
public class DashboardOverviewDto
{
    /// <summary>
    /// Total number of active tenants
    /// </summary>
    public int TotalTenants { get; set; }
    
    /// <summary>
    /// Total number of employees across all tenants
    /// </summary>
    public int TotalEmployees { get; set; }
    
    /// <summary>
    /// Total number of API requests across all tenants (current period)
    /// </summary>
    public long TotalApiRequests { get; set; }
    
    /// <summary>
    /// Total API requests today across all tenants
    /// </summary>
    public long TotalApiRequestsToday { get; set; }
    
    /// <summary>
    /// Total storage used across all tenants (in bytes)
    /// </summary>
    public long TotalStorageBytes { get; set; }
    
    /// <summary>
    /// Total storage used in MB
    /// </summary>
    public double TotalStorageMB => TotalStorageBytes / (1024.0 * 1024.0);
    
    /// <summary>
    /// Total storage used in GB
    /// </summary>
    public double TotalStorageGB => TotalStorageBytes / (1024.0 * 1024.0 * 1024.0);
    
    /// <summary>
    /// Per-tenant breakdown for table view
    /// </summary>
    public List<TenantUsageDashboardDto> Tenants { get; set; } = new();
    
    /// <summary>
    /// Current period start date
    /// </summary>
    public DateTime PeriodStart { get; set; }
    
    /// <summary>
    /// Current period end date
    /// </summary>
    public DateTime PeriodEnd { get; set; }
    
    /// <summary>
    /// Comparison with previous period (trends)
    /// </summary>
    public UsageTrendDto? Trend { get; set; }
    
    /// <summary>
    /// Summary of active alerts by severity
    /// </summary>
    public AlertsSummaryDto AlertsSummary { get; set; } = new();
    
    /// <summary>
    /// Top tenants by usage (sorted by total usage score)
    /// </summary>
    public List<TenantUsageDashboardDto> TopTenantsByUsage { get; set; } = new();
}

/// <summary>
/// Per-tenant usage dashboard DTO
/// </summary>
public class TenantUsageDashboardDto
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string SubscriptionPlan { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    
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
    
    // Active alerts count
    public int ActiveAlertsCount { get; set; }
    public int CriticalAlertsCount { get; set; }
    public int WarningAlertsCount { get; set; }
    
    // Period
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Usage trend comparison DTO (current vs previous period)
/// </summary>
public class UsageTrendDto
{
    public EmployeeTrendDto Employees { get; set; } = new();
    public ApiRequestsTrendDto ApiRequests { get; set; } = new();
    public StorageTrendDto Storage { get; set; } = new();
}

public class EmployeeTrendDto
{
    public int CurrentPeriod { get; set; }
    public int PreviousPeriod { get; set; }
    public int Change => CurrentPeriod - PreviousPeriod;
    public double ChangePercent => PreviousPeriod > 0 ? (Change * 100.0 / PreviousPeriod) : 0;
}

public class ApiRequestsTrendDto
{
    public long CurrentPeriod { get; set; }
    public long PreviousPeriod { get; set; }
    public long Change => CurrentPeriod - PreviousPeriod;
    public double ChangePercent => PreviousPeriod > 0 ? (Change * 100.0 / PreviousPeriod) : 0;
}

public class StorageTrendDto
{
    public long CurrentPeriod { get; set; }
    public long PreviousPeriod { get; set; }
    public long Change => CurrentPeriod - PreviousPeriod;
    public double ChangePercent => PreviousPeriod > 0 ? (Change * 100.0 / PreviousPeriod) : 0;
}

/// <summary>
/// Alerts summary DTO
/// </summary>
public class AlertsSummaryDto
{
    public int TotalActive { get; set; }
    public int Critical { get; set; }
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
}

