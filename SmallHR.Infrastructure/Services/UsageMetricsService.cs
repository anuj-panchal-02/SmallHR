using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Core.DTOs.UsageMetrics;
using SmallHR.Infrastructure.Data;
using System.Linq;
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

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var now = DateTime.UtcNow;
        
        // Determine period dates
        var periodStart = startDate ?? new DateTime(now.Year, now.Month, 1);
        var periodEnd = endDate ?? periodStart.AddMonths(1).AddDays(-1);
        
        // Previous period for comparison
        var previousPeriodStart = periodStart.AddMonths(-1);
        var previousPeriodEnd = periodStart.AddDays(-1);
        
        // Get all active tenants (Tenants table has no tenant filter, so direct query is fine)
        var allTenants = await _context.Tenants
            .Where(t => t.IsActive && !t.IsDeleted)
            .ToListAsync();
        
        var tenantsList = new List<TenantUsageDashboardDto>();
        long totalEmployees = 0;
        long totalApiRequests = 0;
        long totalApiRequestsToday = 0;
        long totalStorageBytes = 0;
        
        // Previous period totals for trends
        long previousTotalEmployees = 0;
        long previousTotalApiRequests = 0;
        long previousTotalStorageBytes = 0;
        
        // Get all active alerts to summarize
        var allActiveAlerts = await _context.Alerts
            .Where(a => a.Status == "Active" && !a.IsDeleted)
            .ToListAsync();
        
        var alertsSummary = new AlertsSummaryDto
        {
            TotalActive = allActiveAlerts.Count,
            Critical = allActiveAlerts.Count(a => a.Severity == "Critical"),
            High = allActiveAlerts.Count(a => a.Severity == "High"),
            Medium = allActiveAlerts.Count(a => a.Severity == "Medium"),
            Low = allActiveAlerts.Count(a => a.Severity == "Low")
        };
        
        // Process each tenant
        foreach (var tenant in allTenants)
        {
            // Get current period metrics
            var currentMetrics = await _context.TenantUsageMetrics
                .FirstOrDefaultAsync(m => m.TenantId == tenant.Id && 
                                         m.PeriodStart == periodStart);
            
            // Get previous period metrics for trend calculation
            var previousMetrics = await _context.TenantUsageMetrics
                .FirstOrDefaultAsync(m => m.TenantId == tenant.Id && 
                                         m.PeriodStart == previousPeriodStart);
            
            // If no metrics exist for current period, initialize from database
            if (currentMetrics == null)
            {
                // Get actual employee count (ignore tenant filters for SuperAdmin aggregation)
                // Note: Employee.TenantId is stored as string (tenant ID as string), not tenant name
                var tenantIdString = tenant.Id.ToString();
                var employeeCount = await _context.Employees
                    .IgnoreQueryFilters()
                    .Where(e => e.TenantId == tenantIdString && !e.IsDeleted)
                    .CountAsync();
                
                // User.TenantId is also stored as string (tenant ID as string), not tenant name
                var userCount = await _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.TenantId == tenantIdString && u.IsActive && !string.IsNullOrEmpty(u.TenantId))
                    .CountAsync();
                
                currentMetrics = new TenantUsageMetrics
                {
                    TenantId = tenant.Id,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    EmployeeCount = employeeCount,
                    UserCount = userCount,
                    ApiRequestCount = 0,
                    ApiRequestCountToday = 0,
                    StorageBytesUsed = 0,
                    LastUpdated = now
                };
            }
            
            // Deserialize FeatureUsage if needed
            if (currentMetrics.FeatureUsage.Count == 0 && !string.IsNullOrWhiteSpace(currentMetrics.FeatureUsageJson))
            {
                try
                {
                    currentMetrics.FeatureUsage = JsonSerializer.Deserialize<Dictionary<string, int>>(currentMetrics.FeatureUsageJson) 
                        ?? new Dictionary<string, int>();
                }
                catch
                {
                    currentMetrics.FeatureUsage = new Dictionary<string, int>();
                }
            }
            
            // Get subscription and plan info
            var subscription = await _subscriptionService.GetSubscriptionByTenantIdAsync(tenant.Id);
            var plan = subscription != null 
                ? await _subscriptionService.GetPlanByIdAsync(subscription.SubscriptionPlanId)
                : null;
            
            // Get API rate limit based on plan
            var apiLimitPerDay = plan?.Name.ToUpperInvariant() switch
            {
                "FREE" => 1000,
                "BASIC" => 10000,
                "PRO" => 100000,
                "ENTERPRISE" => 1000000,
                _ => 10000 // Default
            };
            
            // Get alerts for this tenant
            var tenantAlerts = allActiveAlerts.Where(a => a.TenantId == tenant.Id).ToList();
            
            // Build tenant dashboard DTO
            var tenantDashboard = new TenantUsageDashboardDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                SubscriptionPlan = plan?.Name ?? tenant.SubscriptionPlan,
                IsActive = tenant.IsActive && tenant.Status == TenantStatus.Active,
                EmployeeCount = currentMetrics.EmployeeCount,
                EmployeeLimit = plan?.MaxEmployees ?? tenant.MaxEmployees,
                UserCount = currentMetrics.UserCount,
                UserLimit = plan?.MaxUsers,
                StorageBytesUsed = currentMetrics.StorageBytesUsed,
                StorageLimitBytes = plan?.MaxStorageBytes,
                ApiRequestsThisPeriod = currentMetrics.ApiRequestCount,
                ApiRequestsToday = currentMetrics.ApiRequestCountToday,
                ApiLimitPerDay = apiLimitPerDay,
                ActiveAlertsCount = tenantAlerts.Count,
                CriticalAlertsCount = tenantAlerts.Count(a => a.Severity == "Critical"),
                WarningAlertsCount = tenantAlerts.Count(a => a.Severity == "High" || a.Severity == "Medium"),
                PeriodStart = currentMetrics.PeriodStart,
                PeriodEnd = currentMetrics.PeriodEnd
            };
            
            tenantsList.Add(tenantDashboard);
            
            // Accumulate totals
            totalEmployees += currentMetrics.EmployeeCount;
            totalApiRequests += currentMetrics.ApiRequestCount;
            totalApiRequestsToday += currentMetrics.ApiRequestCountToday;
            totalStorageBytes += currentMetrics.StorageBytesUsed;
            
            // Accumulate previous period totals
            if (previousMetrics != null)
            {
                previousTotalEmployees += previousMetrics.EmployeeCount;
                previousTotalApiRequests += previousMetrics.ApiRequestCount;
                previousTotalStorageBytes += previousMetrics.StorageBytesUsed;
            }
        }
        
        // Build trends
        var trend = new UsageTrendDto
        {
            Employees = new EmployeeTrendDto
            {
                CurrentPeriod = (int)totalEmployees,
                PreviousPeriod = (int)previousTotalEmployees
            },
            ApiRequests = new ApiRequestsTrendDto
            {
                CurrentPeriod = totalApiRequests,
                PreviousPeriod = previousTotalApiRequests
            },
            Storage = new StorageTrendDto
            {
                CurrentPeriod = totalStorageBytes,
                PreviousPeriod = previousTotalStorageBytes
            }
        };
        
        // Calculate top tenants by usage (sort by weighted usage score)
        var topTenants = tenantsList
            .OrderByDescending(t => CalculateUsageScore(t))
            .Take(10)
            .ToList();
        
        return new DashboardOverviewDto
        {
            TotalTenants = allTenants.Count,
            TotalEmployees = (int)totalEmployees,
            TotalApiRequests = totalApiRequests,
            TotalApiRequestsToday = totalApiRequestsToday,
            TotalStorageBytes = totalStorageBytes,
            Tenants = tenantsList,
            TopTenantsByUsage = topTenants,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Trend = trend,
            AlertsSummary = alertsSummary
        };
    }
    
    /// <summary>
    /// Calculates a weighted usage score for ranking tenants
    /// </summary>
    private double CalculateUsageScore(TenantUsageDashboardDto tenant)
    {
        // Weighted score: employees (40%), API requests (30%), storage (20%), alerts (10%)
        var employeeScore = tenant.EmployeeCount * 0.4;
        var apiScore = (tenant.ApiRequestsThisPeriod / 1000.0) * 0.3; // Normalize API requests
        var storageScore = (tenant.StorageBytesUsed / (1024.0 * 1024.0 * 1024.0)) * 0.2; // Normalize to GB
        var alertScore = (tenant.CriticalAlertsCount * 10 + tenant.WarningAlertsCount * 5) * 0.1;
        
        return employeeScore + apiScore + storageScore + alertScore;
    }

    public async Task<TenantDashboardDto> GetTenantDashboardAsync(int tenantId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with ID {tenantId} not found");
        }
        
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        
        // Get current metrics
        var currentMetrics = await GetCurrentMetricsAsync(tenantId);
        
        // Deserialize FeatureUsage if needed
        if (currentMetrics.FeatureUsage.Count == 0 && !string.IsNullOrWhiteSpace(currentMetrics.FeatureUsageJson))
        {
            try
            {
                currentMetrics.FeatureUsage = JsonSerializer.Deserialize<Dictionary<string, int>>(currentMetrics.FeatureUsageJson) 
                    ?? new Dictionary<string, int>();
            }
            catch
            {
                currentMetrics.FeatureUsage = new Dictionary<string, int>();
            }
        }
        
        // Get subscription and plan info
        var subscription = await _subscriptionService.GetSubscriptionByTenantIdAsync(tenantId);
        var plan = subscription != null 
            ? await _subscriptionService.GetPlanByIdAsync(subscription.SubscriptionPlanId)
            : null;
        
        // Get API rate limit based on plan
        var apiLimitPerDay = plan?.Name.ToUpperInvariant() switch
        {
            "FREE" => 1000,
            "BASIC" => 10000,
            "PRO" => 100000,
            "ENTERPRISE" => 1000000,
            _ => 10000 // Default
        };
        
        // Get active alerts for tenant
        var activeAlertsCount = await _context.Alerts
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId && a.Status == "Active" && !a.IsDeleted)
            .CountAsync();
        
        // Build current usage summary
        var currentUsage = new TenantUsageSummaryDto
        {
            EmployeeCount = currentMetrics.EmployeeCount,
            EmployeeLimit = plan?.MaxEmployees ?? tenant.MaxEmployees,
            UserCount = currentMetrics.UserCount,
            UserLimit = plan?.MaxUsers,
            StorageBytesUsed = currentMetrics.StorageBytesUsed,
            StorageLimitBytes = plan?.MaxStorageBytes,
            ApiRequestsThisPeriod = currentMetrics.ApiRequestCount,
            ApiRequestsToday = currentMetrics.ApiRequestCountToday,
            ApiLimitPerDay = apiLimitPerDay,
            ActiveAlertsCount = activeAlertsCount,
            PeriodStart = currentMetrics.PeriodStart,
            PeriodEnd = currentMetrics.PeriodEnd
        };
        
        // Get daily trends (last 7 days)
        var dailyTrends = await GetDailyTrendsAsync(tenantId, 7);
        
        // Get weekly trends (last 4 weeks)
        var weeklyTrends = await GetWeeklyTrendsAsync(tenantId, 4);
        
        // Get monthly trends (last 12 months)
        var monthlyTrends = await GetMonthlyTrendsAsync(tenantId, 12);
        
        // Build feature usage breakdown
        var featureUsage = currentMetrics.FeatureUsage.Select(f => new FeatureUsageDto
        {
            FeatureName = f.Key,
            UsageCount = f.Value,
            Limit = null // Feature limits would come from subscription plan features
        }).ToList();
        
        return new TenantDashboardDto
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            SubscriptionPlan = plan?.Name ?? tenant.SubscriptionPlan,
            IsActive = tenant.IsActive && tenant.Status == TenantStatus.Active,
            CurrentUsage = currentUsage,
            DailyTrends = dailyTrends,
            WeeklyTrends = weeklyTrends,
            MonthlyTrends = monthlyTrends,
            FeatureUsage = featureUsage
        };
    }
    
    private async Task<List<DailyUsagePointDto>> GetDailyTrendsAsync(int tenantId, int days)
    {
        var now = DateTime.UtcNow.Date;
        var trends = new List<DailyUsagePointDto>();
        
        for (int i = days - 1; i >= 0; i--)
        {
            var date = now.AddDays(-i);
            
            // Get metrics for this day if available
            var dayMetrics = await _context.TenantUsageMetrics
                .Where(m => m.TenantId == tenantId && 
                           m.PeriodStart.Date <= date && 
                           m.PeriodEnd.Date >= date)
                .FirstOrDefaultAsync();
            
            // If no metrics found, query actual counts (ignoring tenant filters for SuperAdmin)
            var tenant = await _context.Tenants.FindAsync(tenantId);
            var tenantIdString = tenant?.Id.ToString() ?? tenantId.ToString();
            
            var employeeCount = dayMetrics?.EmployeeCount ?? await _context.Employees
                .IgnoreQueryFilters()
                .Where(e => e.TenantId == tenantIdString && 
                           !e.IsDeleted &&
                           e.CreatedAt.Date <= date)
                .CountAsync();
            
            var userCount = dayMetrics?.UserCount ?? await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenantIdString && 
                           u.IsActive && 
                           !string.IsNullOrEmpty(u.TenantId) &&
                           u.CreatedAt.Date <= date)
                .CountAsync();
            
            // For API requests, we'd need daily tracking - for now use period metrics if available
            var apiRequests = dayMetrics?.ApiRequestCount ?? 0;
            
            // For storage, use current value (storage doesn't typically decrease)
            var storageBytes = dayMetrics?.StorageBytesUsed ?? 0;
            
            trends.Add(new DailyUsagePointDto
            {
                Date = date,
                EmployeeCount = employeeCount,
                UserCount = userCount,
                ApiRequests = apiRequests,
                StorageBytes = storageBytes
            });
        }
        
        return trends;
    }
    
    private async Task<List<WeeklyUsagePointDto>> GetWeeklyTrendsAsync(int tenantId, int weeks)
    {
        var now = DateTime.UtcNow;
        var trends = new List<WeeklyUsagePointDto>();
        
        for (int i = weeks - 1; i >= 0; i--)
        {
            var weekStart = now.AddDays(-(i * 7)).Date;
            // Start of week (Monday)
            weekStart = weekStart.AddDays(-(int)weekStart.DayOfWeek + 1);
            var weekEnd = weekStart.AddDays(6);
            
            // Get metrics for this week
            var weekMetrics = await _context.TenantUsageMetrics
                .Where(m => m.TenantId == tenantId && 
                           m.PeriodStart.Date <= weekEnd && 
                           m.PeriodEnd.Date >= weekStart)
                .FirstOrDefaultAsync();
            
            var tenant = await _context.Tenants.FindAsync(tenantId);
            var tenantIdString = tenant?.Id.ToString() ?? tenantId.ToString();
            var employeeCount = weekMetrics?.EmployeeCount ?? await _context.Employees
                .IgnoreQueryFilters()
                .Where(e => e.TenantId == tenantIdString && 
                           !e.IsDeleted &&
                           e.CreatedAt.Date <= weekEnd)
                .CountAsync();
            
            var userCount = weekMetrics?.UserCount ?? await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenantIdString && 
                           u.IsActive && 
                           !string.IsNullOrEmpty(u.TenantId) &&
                           u.CreatedAt.Date <= weekEnd)
                .CountAsync();
            
            var apiRequests = weekMetrics?.ApiRequestCount ?? 0;
            var storageBytes = weekMetrics?.StorageBytesUsed ?? 0;
            
            trends.Add(new WeeklyUsagePointDto
            {
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                EmployeeCount = employeeCount,
                UserCount = userCount,
                ApiRequests = apiRequests,
                StorageBytes = storageBytes
            });
        }
        
        return trends;
    }
    
    private async Task<List<MonthlyUsagePointDto>> GetMonthlyTrendsAsync(int tenantId, int months)
    {
        var now = DateTime.UtcNow;
        var trends = new List<MonthlyUsagePointDto>();
        
        for (int i = months - 1; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            
            // Get metrics for this month
            var monthMetrics = await _context.TenantUsageMetrics
                .Where(m => m.TenantId == tenantId && 
                           m.PeriodStart.Year == monthStart.Year && 
                           m.PeriodStart.Month == monthStart.Month)
                .FirstOrDefaultAsync();
            
            var tenant = await _context.Tenants.FindAsync(tenantId);
            var employeeCount = monthMetrics?.EmployeeCount ?? await _context.Employees
                .IgnoreQueryFilters()
                .Where(e => e.TenantId == tenant!.Name && 
                           !e.IsDeleted &&
                           e.CreatedAt.Date <= monthEnd)
                .CountAsync();
            
            var userCount = monthMetrics?.UserCount ?? await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenant!.Name && 
                           u.IsActive && 
                           !string.IsNullOrEmpty(u.TenantId) &&
                           u.CreatedAt.Date <= monthEnd)
                .CountAsync();
            
            var apiRequests = monthMetrics?.ApiRequestCount ?? 0;
            var storageBytes = monthMetrics?.StorageBytesUsed ?? 0;
            
            trends.Add(new MonthlyUsagePointDto
            {
                MonthStart = monthStart,
                MonthEnd = monthEnd,
                EmployeeCount = employeeCount,
                UserCount = userCount,
                ApiRequests = apiRequests,
                StorageBytes = storageBytes
            });
        }
        
        return trends;
    }

    public async Task<UsageHistoryDto> GetUsageHistoryAsync(int? tenantId, DateTime startDate, DateTime endDate, string granularity)
    {
        granularity = granularity.ToLowerInvariant();
        if (!new[] { "daily", "weekly", "monthly" }.Contains(granularity))
        {
            throw new ArgumentException("Granularity must be 'daily', 'weekly', or 'monthly'", nameof(granularity));
        }
        
        var dataPoints = new List<UsageHistoryPointDto>();
        
        if (tenantId.HasValue)
        {
            // Get data for specific tenant
            var tenant = await _context.Tenants.FindAsync(tenantId.Value);
            if (tenant == null)
            {
                throw new KeyNotFoundException($"Tenant with ID {tenantId.Value} not found");
            }
            
            dataPoints = granularity switch
            {
                "daily" => await GetDailyHistoryPointsAsync(tenantId.Value, startDate, endDate),
                "weekly" => await GetWeeklyHistoryPointsAsync(tenantId.Value, startDate, endDate),
                "monthly" => await GetMonthlyHistoryPointsAsync(tenantId.Value, startDate, endDate),
                _ => throw new ArgumentException("Invalid granularity")
            };
            
            return new UsageHistoryDto
            {
                TenantId = tenantId.Value,
                TenantName = tenant.Name,
                StartDate = startDate,
                EndDate = endDate,
                Granularity = granularity,
                DataPoints = dataPoints
            };
        }
        else
        {
            // Get aggregated data across all tenants (SuperAdmin view)
            dataPoints = granularity switch
            {
                "daily" => await GetDailyHistoryPointsAggregatedAsync(startDate, endDate),
                "weekly" => await GetWeeklyHistoryPointsAggregatedAsync(startDate, endDate),
                "monthly" => await GetMonthlyHistoryPointsAggregatedAsync(startDate, endDate),
                _ => throw new ArgumentException("Invalid granularity")
            };
            
            return new UsageHistoryDto
            {
                TenantId = null,
                TenantName = null,
                StartDate = startDate,
                EndDate = endDate,
                Granularity = granularity,
                DataPoints = dataPoints
            };
        }
    }
    
    private async Task<List<UsageHistoryPointDto>> GetDailyHistoryPointsAsync(int tenantId, DateTime startDate, DateTime endDate)
    {
        var points = new List<UsageHistoryPointDto>();
        var currentDate = startDate.Date;
        
        while (currentDate <= endDate.Date)
        {
            var periodEnd = currentDate.Date;
            
            // Get metrics for this day
            var dayMetrics = await _context.TenantUsageMetrics
                .Where(m => m.TenantId == tenantId && 
                           m.PeriodStart.Date <= periodEnd && 
                           m.PeriodEnd.Date >= currentDate)
                .FirstOrDefaultAsync();
            
            var tenant = await _context.Tenants.FindAsync(tenantId);
            var tenantIdString = tenant?.Id.ToString() ?? tenantId.ToString();
            var employeeCount = dayMetrics?.EmployeeCount ?? await _context.Employees
                .IgnoreQueryFilters()
                .Where(e => e.TenantId == tenantIdString && !e.IsDeleted && e.CreatedAt.Date <= periodEnd)
                .CountAsync();
            
            var userCount = dayMetrics?.UserCount ?? await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenantIdString && u.IsActive && !string.IsNullOrEmpty(u.TenantId) && u.CreatedAt.Date <= periodEnd)
                .CountAsync();
            
            var apiRequests = dayMetrics?.ApiRequestCount ?? 0;
            var storageBytes = dayMetrics?.StorageBytesUsed ?? 0;
            
            // Deserialize feature usage if available
            var featureUsage = new Dictionary<string, int>();
            if (dayMetrics != null && !string.IsNullOrWhiteSpace(dayMetrics.FeatureUsageJson))
            {
                try
                {
                    featureUsage = JsonSerializer.Deserialize<Dictionary<string, int>>(dayMetrics.FeatureUsageJson) 
                        ?? new Dictionary<string, int>();
                }
                catch
                {
                    featureUsage = new Dictionary<string, int>();
                }
            }
            
            points.Add(new UsageHistoryPointDto
            {
                Timestamp = currentDate,
                PeriodStart = currentDate,
                PeriodEnd = periodEnd,
                EmployeeCount = employeeCount,
                UserCount = userCount,
                ApiRequests = apiRequests,
                StorageBytes = storageBytes,
                FeatureUsage = featureUsage
            });
            
            currentDate = currentDate.AddDays(1);
        }
        
        return points;
    }
    
    private async Task<List<UsageHistoryPointDto>> GetWeeklyHistoryPointsAsync(int tenantId, DateTime startDate, DateTime endDate)
    {
        var points = new List<UsageHistoryPointDto>();
        var currentDate = startDate.Date;
        
        while (currentDate <= endDate.Date)
        {
            var weekStart = currentDate;
            // Start of week (Monday)
            weekStart = weekStart.AddDays(-(int)weekStart.DayOfWeek + 1);
            var weekEnd = weekStart.AddDays(6);
            
            if (weekEnd > endDate) weekEnd = endDate;
            
            var weekMetrics = await _context.TenantUsageMetrics
                .Where(m => m.TenantId == tenantId && 
                           m.PeriodStart.Date <= weekEnd && 
                           m.PeriodEnd.Date >= weekStart)
                .FirstOrDefaultAsync();
            
            var tenant = await _context.Tenants.FindAsync(tenantId);
            var employeeCount = weekMetrics?.EmployeeCount ?? await _context.Employees
                .IgnoreQueryFilters()
                .Where(e => e.TenantId == tenant!.Name && !e.IsDeleted && e.CreatedAt.Date <= weekEnd)
                .CountAsync();
            
            var userCount = weekMetrics?.UserCount ?? await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenant!.Name && u.IsActive && !string.IsNullOrEmpty(u.TenantId) && u.CreatedAt.Date <= weekEnd)
                .CountAsync();
            
            var apiRequests = weekMetrics?.ApiRequestCount ?? 0;
            var storageBytes = weekMetrics?.StorageBytesUsed ?? 0;
            
            var featureUsage = new Dictionary<string, int>();
            if (weekMetrics != null && !string.IsNullOrWhiteSpace(weekMetrics.FeatureUsageJson))
            {
                try
                {
                    featureUsage = JsonSerializer.Deserialize<Dictionary<string, int>>(weekMetrics.FeatureUsageJson) 
                        ?? new Dictionary<string, int>();
                }
                catch
                {
                    featureUsage = new Dictionary<string, int>();
                }
            }
            
            points.Add(new UsageHistoryPointDto
            {
                Timestamp = weekStart,
                PeriodStart = weekStart,
                PeriodEnd = weekEnd,
                EmployeeCount = employeeCount,
                UserCount = userCount,
                ApiRequests = apiRequests,
                StorageBytes = storageBytes,
                FeatureUsage = featureUsage
            });
            
            currentDate = weekEnd.AddDays(1);
        }
        
        return points;
    }
    
    private async Task<List<UsageHistoryPointDto>> GetMonthlyHistoryPointsAsync(int tenantId, DateTime startDate, DateTime endDate)
    {
        var points = new List<UsageHistoryPointDto>();
        var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
        
        while (currentDate <= endDate)
        {
            var monthStart = currentDate;
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            if (monthEnd > endDate) monthEnd = endDate;
            
            var monthMetrics = await _context.TenantUsageMetrics
                .Where(m => m.TenantId == tenantId && 
                           m.PeriodStart.Year == monthStart.Year && 
                           m.PeriodStart.Month == monthStart.Month)
                .FirstOrDefaultAsync();
            
            var tenant = await _context.Tenants.FindAsync(tenantId);
            var tenantIdString = tenant?.Id.ToString() ?? tenantId.ToString();
            var employeeCount = monthMetrics?.EmployeeCount ?? await _context.Employees
                .IgnoreQueryFilters()
                .Where(e => e.TenantId == tenantIdString && !e.IsDeleted && e.CreatedAt.Date <= monthEnd)
                .CountAsync();
            
            var userCount = monthMetrics?.UserCount ?? await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenantIdString && u.IsActive && !string.IsNullOrEmpty(u.TenantId) && u.CreatedAt.Date <= monthEnd)
                .CountAsync();
            
            var apiRequests = monthMetrics?.ApiRequestCount ?? 0;
            var storageBytes = monthMetrics?.StorageBytesUsed ?? 0;
            
            var featureUsage = new Dictionary<string, int>();
            if (monthMetrics != null && !string.IsNullOrWhiteSpace(monthMetrics.FeatureUsageJson))
            {
                try
                {
                    featureUsage = JsonSerializer.Deserialize<Dictionary<string, int>>(monthMetrics.FeatureUsageJson) 
                        ?? new Dictionary<string, int>();
                }
                catch
                {
                    featureUsage = new Dictionary<string, int>();
                }
            }
            
            points.Add(new UsageHistoryPointDto
            {
                Timestamp = monthStart,
                PeriodStart = monthStart,
                PeriodEnd = monthEnd,
                EmployeeCount = employeeCount,
                UserCount = userCount,
                ApiRequests = apiRequests,
                StorageBytes = storageBytes,
                FeatureUsage = featureUsage
            });
            
            currentDate = monthStart.AddMonths(1);
        }
        
        return points;
    }
    
    private async Task<List<UsageHistoryPointDto>> GetDailyHistoryPointsAggregatedAsync(DateTime startDate, DateTime endDate)
    {
        var points = new List<UsageHistoryPointDto>();
        var currentDate = startDate.Date;
        
        while (currentDate <= endDate.Date)
        {
            // Aggregate across all tenants
            var allTenants = await _context.Tenants
                .Where(t => t.IsActive && !t.IsDeleted)
                .ToListAsync();
            
            int totalEmployees = 0;
            int totalUsers = 0;
            long totalApiRequests = 0;
            long totalStorageBytes = 0;
            var aggregatedFeatureUsage = new Dictionary<string, int>();
            
            foreach (var tenant in allTenants)
            {
                var dayMetrics = await _context.TenantUsageMetrics
                    .Where(m => m.TenantId == tenant.Id && 
                               m.PeriodStart.Date <= currentDate && 
                               m.PeriodEnd.Date >= currentDate)
                    .FirstOrDefaultAsync();
                
                if (dayMetrics != null)
                {
                    totalEmployees += dayMetrics.EmployeeCount;
                    totalUsers += dayMetrics.UserCount;
                    totalApiRequests += dayMetrics.ApiRequestCount;
                    totalStorageBytes += dayMetrics.StorageBytesUsed;
                    
                    if (!string.IsNullOrWhiteSpace(dayMetrics.FeatureUsageJson))
                    {
                        try
                        {
                            var tenantFeatureUsage = JsonSerializer.Deserialize<Dictionary<string, int>>(dayMetrics.FeatureUsageJson) 
                                ?? new Dictionary<string, int>();
                            foreach (var feature in tenantFeatureUsage)
                            {
                                aggregatedFeatureUsage[feature.Key] = aggregatedFeatureUsage.GetValueOrDefault(feature.Key, 0) + feature.Value;
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    // Fallback to actual counts
                    totalEmployees += await _context.Employees
                        .IgnoreQueryFilters()
                        .Where(e => e.TenantId == tenant.Id.ToString() && !e.IsDeleted && e.CreatedAt.Date <= currentDate)
                        .CountAsync();
                    
                    totalUsers += await _context.Users
                        .IgnoreQueryFilters()
                        .Where(u => u.TenantId == tenant.Id.ToString() && u.IsActive && !string.IsNullOrEmpty(u.TenantId) && u.CreatedAt.Date <= currentDate)
                        .CountAsync();
                }
            }
            
            points.Add(new UsageHistoryPointDto
            {
                Timestamp = currentDate,
                PeriodStart = currentDate,
                PeriodEnd = currentDate,
                EmployeeCount = totalEmployees,
                UserCount = totalUsers,
                ApiRequests = totalApiRequests,
                StorageBytes = totalStorageBytes,
                FeatureUsage = aggregatedFeatureUsage
            });
            
            currentDate = currentDate.AddDays(1);
        }
        
        return points;
    }
    
    private async Task<List<UsageHistoryPointDto>> GetWeeklyHistoryPointsAggregatedAsync(DateTime startDate, DateTime endDate)
    {
        var points = new List<UsageHistoryPointDto>();
        var currentDate = startDate.Date;
        
        while (currentDate <= endDate.Date)
        {
            var weekStart = currentDate;
            weekStart = weekStart.AddDays(-(int)weekStart.DayOfWeek + 1);
            var weekEnd = weekStart.AddDays(6);
            if (weekEnd > endDate) weekEnd = endDate;
            
            var allTenants = await _context.Tenants
                .Where(t => t.IsActive && !t.IsDeleted)
                .ToListAsync();
            
            int totalEmployees = 0;
            int totalUsers = 0;
            long totalApiRequests = 0;
            long totalStorageBytes = 0;
            var aggregatedFeatureUsage = new Dictionary<string, int>();
            
            foreach (var tenant in allTenants)
            {
                var weekMetrics = await _context.TenantUsageMetrics
                    .Where(m => m.TenantId == tenant.Id && 
                               m.PeriodStart.Date <= weekEnd && 
                               m.PeriodEnd.Date >= weekStart)
                    .FirstOrDefaultAsync();
                
                if (weekMetrics != null)
                {
                    totalEmployees += weekMetrics.EmployeeCount;
                    totalUsers += weekMetrics.UserCount;
                    totalApiRequests += weekMetrics.ApiRequestCount;
                    totalStorageBytes += weekMetrics.StorageBytesUsed;
                    
                    if (!string.IsNullOrWhiteSpace(weekMetrics.FeatureUsageJson))
                    {
                        try
                        {
                            var tenantFeatureUsage = JsonSerializer.Deserialize<Dictionary<string, int>>(weekMetrics.FeatureUsageJson) 
                                ?? new Dictionary<string, int>();
                            foreach (var feature in tenantFeatureUsage)
                            {
                                aggregatedFeatureUsage[feature.Key] = aggregatedFeatureUsage.GetValueOrDefault(feature.Key, 0) + feature.Value;
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    totalEmployees += await _context.Employees
                        .IgnoreQueryFilters()
                        .Where(e => e.TenantId == tenant.Id.ToString() && !e.IsDeleted && e.CreatedAt.Date <= weekEnd)
                        .CountAsync();
                    
                    totalUsers += await _context.Users
                        .IgnoreQueryFilters()
                        .Where(u => u.TenantId == tenant.Id.ToString() && u.IsActive && !string.IsNullOrEmpty(u.TenantId) && u.CreatedAt.Date <= weekEnd)
                        .CountAsync();
                }
            }
            
            points.Add(new UsageHistoryPointDto
            {
                Timestamp = weekStart,
                PeriodStart = weekStart,
                PeriodEnd = weekEnd,
                EmployeeCount = totalEmployees,
                UserCount = totalUsers,
                ApiRequests = totalApiRequests,
                StorageBytes = totalStorageBytes,
                FeatureUsage = aggregatedFeatureUsage
            });
            
            currentDate = weekEnd.AddDays(1);
        }
        
        return points;
    }
    
    private async Task<List<UsageHistoryPointDto>> GetMonthlyHistoryPointsAggregatedAsync(DateTime startDate, DateTime endDate)
    {
        var points = new List<UsageHistoryPointDto>();
        var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
        
        while (currentDate <= endDate)
        {
            var monthStart = currentDate;
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            if (monthEnd > endDate) monthEnd = endDate;
            
            var allTenants = await _context.Tenants
                .Where(t => t.IsActive && !t.IsDeleted)
                .ToListAsync();
            
            int totalEmployees = 0;
            int totalUsers = 0;
            long totalApiRequests = 0;
            long totalStorageBytes = 0;
            var aggregatedFeatureUsage = new Dictionary<string, int>();
            
            foreach (var tenant in allTenants)
            {
                var monthMetrics = await _context.TenantUsageMetrics
                    .Where(m => m.TenantId == tenant.Id && 
                               m.PeriodStart.Year == monthStart.Year && 
                               m.PeriodStart.Month == monthStart.Month)
                    .FirstOrDefaultAsync();
                
                if (monthMetrics != null)
                {
                    totalEmployees += monthMetrics.EmployeeCount;
                    totalUsers += monthMetrics.UserCount;
                    totalApiRequests += monthMetrics.ApiRequestCount;
                    totalStorageBytes += monthMetrics.StorageBytesUsed;
                    
                    if (!string.IsNullOrWhiteSpace(monthMetrics.FeatureUsageJson))
                    {
                        try
                        {
                            var tenantFeatureUsage = JsonSerializer.Deserialize<Dictionary<string, int>>(monthMetrics.FeatureUsageJson) 
                                ?? new Dictionary<string, int>();
                            foreach (var feature in tenantFeatureUsage)
                            {
                                aggregatedFeatureUsage[feature.Key] = aggregatedFeatureUsage.GetValueOrDefault(feature.Key, 0) + feature.Value;
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    totalEmployees += await _context.Employees
                        .IgnoreQueryFilters()
                        .Where(e => e.TenantId == tenant.Id.ToString() && !e.IsDeleted && e.CreatedAt.Date <= monthEnd)
                        .CountAsync();
                    
                    totalUsers += await _context.Users
                        .IgnoreQueryFilters()
                        .Where(u => u.TenantId == tenant.Id.ToString() && u.IsActive && !string.IsNullOrEmpty(u.TenantId) && u.CreatedAt.Date <= monthEnd)
                        .CountAsync();
                }
            }
            
            points.Add(new UsageHistoryPointDto
            {
                Timestamp = monthStart,
                PeriodStart = monthStart,
                PeriodEnd = monthEnd,
                EmployeeCount = totalEmployees,
                UserCount = totalUsers,
                ApiRequests = totalApiRequests,
                StorageBytes = totalStorageBytes,
                FeatureUsage = aggregatedFeatureUsage
            });
            
            currentDate = monthStart.AddMonths(1);
        }
        
        return points;
    }
}

