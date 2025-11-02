using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using System.Text.Json;

namespace SmallHR.Infrastructure.Services;

public class UsageMetricsService : IUsageMetricsService
{
    private readonly ApplicationDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<UsageMetricsService> _logger;
    private readonly ITenantProvider _tenantProvider;

    public UsageMetricsService(
        ApplicationDbContext context,
        ISubscriptionService subscriptionService,
        ILogger<UsageMetricsService> logger,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _subscriptionService = subscriptionService;
        _logger = logger;
        _tenantProvider = tenantProvider;
    }

    public async Task<TenantUsageMetrics> GetCurrentMetricsAsync(int tenantId)
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1); // First day of current month
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var metrics = await _context.TenantUsageMetrics
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && 
                                     m.PeriodStart == periodStart);

        if (metrics == null)
        {
            // Initialize metrics for current period
            // Get actual counts from database
            var actualEmployeeCount = await _context.Employees.CountAsync();
            var actualUserCount = 0; // TODO: Get from User entity when tenant association is added
            
            metrics = new TenantUsageMetrics
            {
                TenantId = tenantId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                EmployeeCount = actualEmployeeCount,
                UserCount = actualUserCount,
                LastUpdated = now
            };

            _context.TenantUsageMetrics.Add(metrics);
            await _context.SaveChangesAsync();
        }
        else if (now.Date != metrics.LastApiRequestDate?.Date)
        {
            // Reset daily counters for new day
            metrics.ApiRequestCountToday = 0;
            metrics.LastApiRequestDate = now.Date;
            await _context.SaveChangesAsync();
        }

        return metrics;
    }

    public async Task IncrementApiRequestCountAsync(int tenantId)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        metrics.ApiRequestCount++;
        metrics.ApiRequestCountToday++;
        metrics.LastApiRequestDate = DateTime.UtcNow.Date;
        metrics.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateEmployeeCountAsync(int tenantId)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        // Get actual count from database (tenant context already applied via query filter)
        var count = await _context.Employees.CountAsync();
        metrics.EmployeeCount = count;
        metrics.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserCountAsync(int tenantId)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        // Note: User count would need tenant association on User entity
        // For now, this is a placeholder
        metrics.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStorageUsageAsync(int tenantId, long bytesAdded)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        metrics.StorageBytesUsed += bytesAdded;
        metrics.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task IncrementFeatureUsageAsync(int tenantId, string featureKey, int count = 1)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        
        // Deserialize FeatureUsage from JSON if needed
        if (metrics.FeatureUsage.Count == 0 && !string.IsNullOrWhiteSpace(metrics.FeatureUsageJson))
        {
            try
            {
                metrics.FeatureUsage = JsonSerializer.Deserialize<Dictionary<string, int>>(metrics.FeatureUsageJson) 
                    ?? new Dictionary<string, int>();
            }
            catch
            {
                metrics.FeatureUsage = new Dictionary<string, int>();
            }
        }
        
        // Update feature usage
        if (metrics.FeatureUsage.ContainsKey(featureKey))
        {
            metrics.FeatureUsage[featureKey] += count;
        }
        else
        {
            metrics.FeatureUsage[featureKey] = count;
        }
        
        // Serialize back to JSON for EF Core
        metrics.FeatureUsageJson = JsonSerializer.Serialize(metrics.FeatureUsage);
        metrics.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CheckEmployeeLimitAsync(int tenantId)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        var limit = await _subscriptionService.GetMaxEmployeesAsync(tenantId);
        return metrics.EmployeeCount < limit;
    }

    public async Task<bool> CheckUserLimitAsync(int tenantId)
    {
        var maxUsers = await _subscriptionService.GetMaxUsersAsync(tenantId);
        if (!maxUsers.HasValue) return true; // No limit

        var metrics = await GetCurrentMetricsAsync(tenantId);
        return metrics.UserCount < maxUsers.Value;
    }

    public async Task<bool> CheckStorageLimitAsync(int tenantId)
    {
        var subscription = await _subscriptionService.GetSubscriptionByTenantIdAsync(tenantId);
        if (subscription == null) return false;

        var plan = await _subscriptionService.GetPlanByIdAsync(subscription.SubscriptionPlanId);
        if (plan == null || !plan.MaxStorageBytes.HasValue) return true; // No limit

        var metrics = await GetCurrentMetricsAsync(tenantId);
        return metrics.StorageBytesUsed < plan.MaxStorageBytes.Value;
    }

    public async Task<bool> CheckApiRateLimitAsync(int tenantId, int limitPerDay)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        return metrics.ApiRequestCountToday < limitPerDay;
    }

    public async Task<int> GetEmployeeCountAsync(int tenantId)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        return metrics.EmployeeCount;
    }

    public async Task<int> GetUserCountAsync(int tenantId)
    {
        // Placeholder - would need tenant association on User entity
        var metrics = await GetCurrentMetricsAsync(tenantId);
        return metrics.UserCount;
    }

    public async Task<long> GetStorageUsageAsync(int tenantId)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        return metrics.StorageBytesUsed;
    }

    public async Task<long> GetApiRequestCountAsync(int tenantId, DateTime? fromDate = null)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        if (fromDate == null || fromDate.Value.Date == DateTime.UtcNow.Date)
        {
            return metrics.ApiRequestCountToday;
        }
        return metrics.ApiRequestCount;
    }

    public async Task<UsageSummaryDto> GetUsageSummaryAsync(int tenantId)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        var subscription = await _subscriptionService.GetSubscriptionByTenantIdAsync(tenantId);
        var plan = subscription != null 
            ? await _subscriptionService.GetPlanByIdAsync(subscription.SubscriptionPlanId)
            : null;

        var tenant = await _context.Tenants.FindAsync(tenantId);

        // Deserialize FeatureUsage if needed
        if (metrics.FeatureUsage.Count == 0 && !string.IsNullOrWhiteSpace(metrics.FeatureUsageJson))
        {
            try
            {
                metrics.FeatureUsage = JsonSerializer.Deserialize<Dictionary<string, int>>(metrics.FeatureUsageJson) 
                    ?? new Dictionary<string, int>();
            }
            catch
            {
                metrics.FeatureUsage = new Dictionary<string, int>();
            }
        }

        // Get API rate limit based on plan
        var apiLimitPerDay = plan?.Name.ToUpperInvariant() switch
        {
            "FREE" => 1000,
            "BASIC" => 10000,
            "PRO" => 100000,
            "ENTERPRISE" => 1000000,
            _ => 10000 // Default
        };

        return new UsageSummaryDto
        {
            TenantId = tenantId,
            TenantName = tenant?.Name ?? string.Empty,
            EmployeeCount = metrics.EmployeeCount,
            EmployeeLimit = plan?.MaxEmployees ?? 0,
            UserCount = metrics.UserCount,
            UserLimit = plan?.MaxUsers,
            StorageBytesUsed = metrics.StorageBytesUsed,
            StorageLimitBytes = plan?.MaxStorageBytes,
            ApiRequestsThisPeriod = metrics.ApiRequestCount,
            ApiRequestsToday = metrics.ApiRequestCountToday,
            ApiLimitPerDay = apiLimitPerDay,
            PeriodStart = metrics.PeriodStart,
            PeriodEnd = metrics.PeriodEnd,
            Limits = new Dictionary<string, object>
            {
                { "maxEmployees", plan?.MaxEmployees ?? 0 },
                { "maxUsers", plan?.MaxUsers ?? (object)"unlimited" },
                { "maxStorageBytes", plan?.MaxStorageBytes ?? (object)"unlimited" },
                { "apiLimitPerDay", apiLimitPerDay }
            },
            Usage = new Dictionary<string, object>
            {
                { "employeeCount", metrics.EmployeeCount },
                { "userCount", metrics.UserCount },
                { "storageBytesUsed", metrics.StorageBytesUsed },
                { "apiRequestsToday", metrics.ApiRequestCountToday },
                { "apiRequestsThisPeriod", metrics.ApiRequestCount },
                { "featureUsage", metrics.FeatureUsage }
            }
        };
    }

    public async Task<Dictionary<string, object>> GetUsageBreakdownAsync(int tenantId)
    {
        var metrics = await GetCurrentMetricsAsync(tenantId);
        var summary = await GetUsageSummaryAsync(tenantId);

        return new Dictionary<string, object>
        {
            { "tenantId", tenantId },
            { "period", new { start = metrics.PeriodStart, end = metrics.PeriodEnd } },
            { "employees", new { current = summary.EmployeeCount, limit = summary.EmployeeLimit, percentage = summary.EmployeeLimit > 0 ? (summary.EmployeeCount * 100.0 / summary.EmployeeLimit) : 0 } },
            { "users", new { current = summary.UserCount, limit = summary.UserLimit ?? -1, percentage = summary.UserLimit.HasValue ? (summary.UserCount * 100.0 / summary.UserLimit.Value) : 0 } },
            { "storage", new { used = summary.StorageBytesUsed, limit = summary.StorageLimitBytes ?? -1, percentage = summary.StorageLimitBytes.HasValue ? (summary.StorageBytesUsed * 100.0 / summary.StorageLimitBytes.Value) : 0 } },
            { "apiRequests", new { today = summary.ApiRequestsToday, limit = summary.ApiLimitPerDay, percentage = (summary.ApiRequestsToday * 100.0 / summary.ApiLimitPerDay) } },
            { "featureUsage", metrics.FeatureUsage }
        };
    }
}

