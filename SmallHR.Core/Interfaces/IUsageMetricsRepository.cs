using SmallHR.Core.DTOs.UsageMetrics;
using SmallHR.Core.Entities;

namespace SmallHR.Core.Interfaces;

public interface IUsageMetricsRepository
{
    Task<List<TenantUsageMetrics>> GetByTenantAndPeriodRangeAsync(int tenantId, DateTime startInclusive, DateTime endInclusive);
    Task<List<TenantUsageMetrics>> GetAllTenantsByPeriodRangeAsync(DateTime startInclusive, DateTime endInclusive);

    /// <summary>
    /// Aggregates metrics grouped by granularity between dates inclusive.
    /// granularity: "daily" | "weekly" | "monthly"
    /// </summary>
    Task<List<HistoricalMetricsPointDto>> AggregateByGranularityAsync(int? tenantId, DateTime startInclusive, DateTime endInclusive, string granularity);
}


