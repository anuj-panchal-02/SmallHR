using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.Entities;
using SmallHR.Infrastructure.Data;
using System.Security.Claims;

namespace SmallHR.API.Controllers;

/// <summary>
/// System-wide Metrics and Analytics for SuperAdmin
/// Provides revenue, churn, usage trends, and business intelligence
/// </summary>
[ApiController]
[Route("api/admin/metrics")]
[AuthorizeSuperAdmin]
public class MetricsAdminController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public MetricsAdminController(
        ApplicationDbContext context,
        ILogger<MetricsAdminController> logger) : base(logger)
    {
        _context = context;
    }

    /// <summary>
    /// Get system-wide metrics overview
    /// </summary>
    [HttpGet("overview")]
    public async Task<ActionResult<object>> GetOverview([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            // Total tenants
            var totalTenants = await _context.Tenants.CountAsync();
            var activeTenants = await _context.Tenants.CountAsync(t => t.IsActive && t.Status == TenantStatus.Active);
            var suspendedTenants = await _context.Tenants.CountAsync(t => t.Status == TenantStatus.Suspended);
            var newTenants = await _context.Tenants.CountAsync(t => t.CreatedAt >= start && t.CreatedAt <= end);

            // Revenue metrics
            var activeSubscriptions = await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.Status == SubscriptionStatus.Active && s.StartDate <= end)
                .ToListAsync();

            var monthlyRecurringRevenue = activeSubscriptions
                .Where(s => s.BillingPeriod == BillingPeriod.Monthly)
                .Sum(s => s.Price);

            var annualRecurringRevenue = activeSubscriptions
                .Where(s => s.BillingPeriod == BillingPeriod.Yearly)
                .Sum(s => s.Price);

            var totalMonthlyRecurringRevenue = monthlyRecurringRevenue + (annualRecurringRevenue / 12);

            // Usage metrics
            var totalUsers = await _context.Users.CountAsync(u => u.TenantId != null);
            var totalEmployees = await _context.Employees.CountAsync();
            
            var totalApiRequests = await _context.TenantUsageMetrics
                .SumAsync(m => m.ApiRequestCount);

            // Subscription distribution
            var subscriptionByPlan = await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.Status == SubscriptionStatus.Active)
                .GroupBy(s => s.Plan!.Name)
                .Select(g => new { planName = g.Key, count = g.Count() })
                .ToListAsync();

            // Tenant status distribution
            var tenantsByStatus = await _context.Tenants
                .GroupBy(t => t.Status)
                .Select(g => new { status = g.Key.ToString(), count = g.Count() })
                .ToListAsync();

            // Recent churn (cancelled subscriptions)
            var churnedSubscriptions = await _context.Subscriptions
                .Include(s => s.Tenant)
                .Include(s => s.Plan)
                .Where(s => s.Status == SubscriptionStatus.Canceled && 
                           s.CanceledAt >= start && s.CanceledAt <= end)
                .ToListAsync();

            var churnedRevenue = churnedSubscriptions.Sum(s => s.Price);
            var churnCount = churnedSubscriptions.Count;

            return new
            {
                period = new { startDate = start, endDate = end },
                tenants = new
                {
                    total = totalTenants,
                    active = activeTenants,
                    suspended = suspendedTenants,
                    newTenants = newTenants,
                    statusDistribution = tenantsByStatus
                },
                revenue = new
                {
                    monthlyRecurringRevenue = totalMonthlyRecurringRevenue,
                    annualRecurringRevenue = annualRecurringRevenue * 12,
                    activeSubscriptions = activeSubscriptions.Count,
                    subscriptionByPlan = subscriptionByPlan
                },
                usage = new
                {
                    totalUsers = totalUsers,
                    totalEmployees = totalEmployees,
                    totalApiRequests = totalApiRequests,
                    averageUsersPerTenant = activeTenants > 0 ? totalUsers / (double)activeTenants : 0,
                    averageEmployeesPerTenant = activeTenants > 0 ? totalEmployees / (double)activeTenants : 0
                },
                churn = new
                {
                    count = churnCount,
                    lostRevenue = churnedRevenue,
                    churnedSubscriptions = churnedSubscriptions.Select(s => new
                    {
                        subscriptionId = s.Id,
                        tenantId = s.TenantId,
                        tenantName = s.Tenant?.Name,
                        planName = s.Plan?.Name,
                        price = s.Price,
                        canceledAt = s.CanceledAt
                    })
                }
            };
            },
            "fetching metrics overview"
        );
    }

    /// <summary>
    /// Get revenue trends over time
    /// </summary>
    [HttpGet("revenue-trends")]
    public async Task<ActionResult<object>> GetRevenueTrends([FromQuery] int months = 12)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var startDate = DateTime.UtcNow.AddMonths(-months);
                var trends = new List<object>();

                for (int i = months - 1; i >= 0; i--)
                {
                    var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-i);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var subscriptions = await _context.Subscriptions
                        .Where(s => s.Status == SubscriptionStatus.Active && s.StartDate <= monthEnd)
                        .ToListAsync();

                    var mrr = subscriptions
                        .Where(s => s.BillingPeriod == BillingPeriod.Monthly && s.StartDate <= monthEnd)
                        .Sum(s => s.Price);

                    var arr = subscriptions
                        .Where(s => s.BillingPeriod == BillingPeriod.Yearly && s.StartDate <= monthEnd)
                        .Sum(s => s.Price / 12);

                    trends.Add(new
                    {
                        month = monthStart.ToString("yyyy-MM"),
                        monthStart = monthStart,
                        monthEnd = monthEnd,
                        monthlyRecurringRevenue = mrr + arr,
                        activeSubscriptions = subscriptions.Count
                    });
                }

                return new { trends = trends };
            },
            "fetching revenue trends"
        );
    }

    /// <summary>
    /// Get tenant growth trends
    /// </summary>
    [HttpGet("tenant-growth")]
    public async Task<ActionResult<object>> GetTenantGrowth([FromQuery] int months = 12)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var startDate = DateTime.UtcNow.AddMonths(-months);
                var trends = new List<object>();

                for (int i = months - 1; i >= 0; i--)
                {
                    var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-i);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var newTenants = await _context.Tenants.CountAsync(t => 
                        t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd);

                    var totalTenants = await _context.Tenants.CountAsync(t => 
                        t.CreatedAt <= monthEnd);

                    var activeTenants = await _context.Tenants.CountAsync(t => 
                        t.CreatedAt <= monthEnd && t.IsActive && t.Status == TenantStatus.Active);

                    trends.Add(new
                    {
                        month = monthStart.ToString("yyyy-MM"),
                        monthStart = monthStart,
                        monthEnd = monthEnd,
                        newTenants = newTenants,
                        totalTenants = totalTenants,
                        activeTenants = activeTenants
                    });
                }

                return new { trends = trends };
            },
            "fetching tenant growth trends"
        );
    }

    /// <summary>
    /// Get churn analysis
    /// </summary>
    [HttpGet("churn-analysis")]
    public async Task<ActionResult<object>> GetChurnAnalysis([FromQuery] int months = 12)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var startDate = DateTime.UtcNow.AddMonths(-months);
                
                var canceledSubscriptions = await _context.Subscriptions
                    .Include(s => s.Tenant)
                    .Include(s => s.Plan)
                    .Where(s => s.Status == SubscriptionStatus.Canceled && 
                               s.CanceledAt >= startDate)
                    .OrderByDescending(s => s.CanceledAt)
                    .ToListAsync();

                var churnByPlan = canceledSubscriptions
                    .GroupBy(s => s.Plan?.Name ?? "Unknown")
                    .Select(g => new { planName = g.Key, count = g.Count(), lostRevenue = g.Sum(s => s.Price) })
                    .ToList();

                var totalActiveAtStart = await _context.Tenants.CountAsync(t => 
                    t.CreatedAt <= startDate && t.Status == TenantStatus.Active);

                var churnRate = totalActiveAtStart > 0 
                    ? (canceledSubscriptions.Count / (double)totalActiveAtStart) * 100 
                    : 0;

                return new
                {
                    period = new { months = months, startDate = startDate },
                    totalChurned = canceledSubscriptions.Count,
                    lostRevenue = canceledSubscriptions.Sum(s => s.Price),
                    churnRate = Math.Round(churnRate, 2),
                    churnByPlan = churnByPlan,
                    recentChurn = canceledSubscriptions.Take(20).Select(s => new
                    {
                        subscriptionId = s.Id,
                        tenantId = s.TenantId,
                        tenantName = s.Tenant?.Name,
                        planName = s.Plan?.Name,
                        price = s.Price,
                        canceledAt = s.CanceledAt,
                        cancellationReason = s.CancellationReason
                    })
                };
            },
            "fetching churn analysis"
        );
    }
}

