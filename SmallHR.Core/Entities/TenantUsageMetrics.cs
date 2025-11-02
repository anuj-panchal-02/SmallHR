namespace SmallHR.Core.Entities;

/// <summary>
/// Tracks usage metrics per tenant for plan limit enforcement
/// </summary>
public class TenantUsageMetrics : BaseEntity
{
    // Foreign Key to Tenant
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;
    
    // Usage Period (e.g., current month)
    public DateTime PeriodStart { get; set; } // Start of billing/usage period
    public DateTime PeriodEnd { get; set; }   // End of billing/usage period
    
    // Current Usage Counts
    public int EmployeeCount { get; set; }
    public int UserCount { get; set; }
    public int DepartmentCount { get; set; }
    public int LeaveRequestCount { get; set; }
    public int AttendanceRecordCount { get; set; }
    
    // API Usage Metrics
    public long ApiRequestCount { get; set; } // Total API requests this period
    public long ApiRequestCountToday { get; set; } // API requests today (for rate limiting)
    public DateTime? LastApiRequestDate { get; set; }
    
    // Storage Metrics
    public long StorageBytesUsed { get; set; } // Total storage used in bytes
    public int FileCount { get; set; }
    
    // Feature Usage
    public Dictionary<string, int> FeatureUsage { get; set; } = new(); // Feature key -> usage count
    public string? FeatureUsageJson { get; set; } // JSON serialized feature usage (for EF Core)
    
    // Bandwidth/Data Transfer (if applicable)
    public long DataTransferBytes { get; set; } // Data transfer in bytes this period
    
    // Metadata
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

