using SmallHR.Core.Entities;
using SmallHR.Core.DTOs.UsageMetrics;

namespace SmallHR.Core.Interfaces;

/// <summary>
/// Service for tracking and enforcing usage metrics per tenant
/// </summary>
public interface IUsageMetricsService
{
    // Metrics Tracking
    Task<TenantUsageMetrics> GetCurrentMetricsAsync(int tenantId);
    Task IncrementApiRequestCountAsync(int tenantId);
    Task UpdateEmployeeCountAsync(int tenantId);
    Task UpdateUserCountAsync(int tenantId);
    Task UpdateStorageUsageAsync(int tenantId, long bytesAdded);
    Task IncrementFeatureUsageAsync(int tenantId, string featureKey, int count = 1);
    
    // Limit Checking
    Task<bool> CheckEmployeeLimitAsync(int tenantId);
    Task<bool> CheckUserLimitAsync(int tenantId);
    Task<bool> CheckStorageLimitAsync(int tenantId);
    Task<bool> CheckApiRateLimitAsync(int tenantId, int limitPerDay);
    
    // Usage Queries
    Task<int> GetEmployeeCountAsync(int tenantId);
    Task<int> GetUserCountAsync(int tenantId);
    Task<long> GetStorageUsageAsync(int tenantId);
    Task<long> GetApiRequestCountAsync(int tenantId, DateTime? fromDate = null);
    
    // Metrics Summary
    Task<UsageSummaryDto> GetUsageSummaryAsync(int tenantId);
    Task<Dictionary<string, object>> GetUsageBreakdownAsync(int tenantId);
    
    // Dashboard Methods
    Task<DashboardOverviewDto> GetDashboardOverviewAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<TenantDashboardDto> GetTenantDashboardAsync(int tenantId);
    Task<UsageHistoryDto> GetUsageHistoryAsync(int? tenantId, DateTime startDate, DateTime endDate, string granularity);
}

/// <summary>
/// Usage summary DTO
/// </summary>
public class UsageSummaryDto
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int EmployeeLimit { get; set; }
    public int UserCount { get; set; }
    public int? UserLimit { get; set; }
    public long StorageBytesUsed { get; set; }
    public long? StorageLimitBytes { get; set; }
    public long ApiRequestsThisPeriod { get; set; }
    public long ApiRequestsToday { get; set; }
    public int ApiLimitPerDay { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Dictionary<string, object> Limits { get; set; } = new();
    public Dictionary<string, object> Usage { get; set; } = new();
}

