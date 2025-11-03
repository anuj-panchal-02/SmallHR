namespace SmallHR.Core.DTOs.UsageMetrics;

public class HistoricalMetricsPointDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int? TenantId { get; set; }

    public int EmployeeCount { get; set; }
    public int UserCount { get; set; }
    public long ApiRequestCount { get; set; }
    public long StorageBytesUsed { get; set; }
}


