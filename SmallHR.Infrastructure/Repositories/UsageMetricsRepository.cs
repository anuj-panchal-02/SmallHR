using Microsoft.EntityFrameworkCore;
using SmallHR.Core.DTOs.UsageMetrics;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Repositories;

public class UsageMetricsRepository : IUsageMetricsRepository
{
    private readonly ApplicationDbContext _context;

    public UsageMetricsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantUsageMetrics>> GetByTenantAndPeriodRangeAsync(int tenantId, DateTime startInclusive, DateTime endInclusive)
    {
        return await _context.TenantUsageMetrics
            .Where(m => m.TenantId == tenantId && m.PeriodStart >= startInclusive && m.PeriodStart <= endInclusive)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<TenantUsageMetrics>> GetAllTenantsByPeriodRangeAsync(DateTime startInclusive, DateTime endInclusive)
    {
        return await _context.TenantUsageMetrics
            .Where(m => m.PeriodStart >= startInclusive && m.PeriodStart <= endInclusive)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<HistoricalMetricsPointDto>> AggregateByGranularityAsync(int? tenantId, DateTime startInclusive, DateTime endInclusive, string granularity)
    {
        granularity = granularity.ToLowerInvariant();
        if (!new[] { "daily", "weekly", "monthly" }.Contains(granularity))
        {
            throw new ArgumentException("Granularity must be 'daily', 'weekly', or 'monthly'", nameof(granularity));
        }

        // Fetch relevant rows
        var baseQuery = _context.TenantUsageMetrics
            .AsNoTracking()
            .Where(m => m.PeriodStart >= startInclusive && m.PeriodStart <= endInclusive);

        if (tenantId.HasValue)
        {
            baseQuery = baseQuery.Where(m => m.TenantId == tenantId.Value);
        }

        var rows = await baseQuery.ToListAsync();

        // Group in-memory by chosen granularity buckets
        DateTime BucketStart(DateTime dt)
        {
            return granularity switch
            {
                "daily" => dt.Date,
                "weekly" => dt.Date.AddDays(-(int)dt.Date.DayOfWeek + 1), // Monday
                "monthly" => new DateTime(dt.Year, dt.Month, 1),
                _ => dt.Date
            };
        }

        DateTime BucketEnd(DateTime start)
        {
            return granularity switch
            {
                "daily" => start,
                "weekly" => start.AddDays(6),
                "monthly" => start.AddMonths(1).AddDays(-1),
                _ => start
            };
        }

        var grouped = rows
            .GroupBy(r => new { Key = BucketStart(r.PeriodStart), r.TenantId })
            .Select(g => new HistoricalMetricsPointDto
            {
                TenantId = tenantId.HasValue ? tenantId.Value : g.Key.TenantId,
                PeriodStart = g.Key.Key,
                PeriodEnd = BucketEnd(g.Key.Key),
                EmployeeCount = g.Sum(x => x.EmployeeCount),
                UserCount = g.Sum(x => x.UserCount),
                ApiRequestCount = g.Sum(x => x.ApiRequestCount),
                StorageBytesUsed = g.Sum(x => x.StorageBytesUsed)
            })
            .OrderBy(p => p.PeriodStart)
            .ToList();

        return grouped;
    }
}


