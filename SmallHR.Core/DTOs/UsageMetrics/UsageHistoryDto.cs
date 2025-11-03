namespace SmallHR.Core.DTOs.UsageMetrics;

/// <summary>
/// Time-series usage history data for charts
/// </summary>
public class UsageHistoryDto
{
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Granularity { get; set; } = "daily"; // daily, weekly, monthly
    
    /// <summary>
    /// Time-series data points
    /// </summary>
    public List<UsageHistoryPointDto> DataPoints { get; set; } = new();
}

/// <summary>
/// Single data point in usage history
/// </summary>
public class UsageHistoryPointDto
{
    public DateTime Timestamp { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    // Metrics at this point in time
    public int EmployeeCount { get; set; }
    public int UserCount { get; set; }
    public long ApiRequests { get; set; }
    public long StorageBytes { get; set; }
    
    // Optional: feature-specific usage
    public Dictionary<string, int> FeatureUsage { get; set; } = new();
}

