using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.Entities;

/// <summary>
/// Alert entity for tracking system alerts (payment failures, overages, errors, suspensions)
/// </summary>
public class Alert : BaseEntity
{
    [Required]
    public int TenantId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AlertType { get; set; } = string.Empty; // PaymentFailure, Overage, Error, Suspension
    
    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Medium"; // High, Medium, Low
    
    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Resolved, Acknowledged
    
    public DateTime? ResolvedAt { get; set; }
    
    [MaxLength(450)]
    public string? ResolvedBy { get; set; } // User ID who resolved the alert
    
    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }
    
    public int? SubscriptionId { get; set; } // Associated subscription if applicable
    
    [MaxLength(4000)]
    public string? MetadataJson { get; set; } // JSON metadata (amount, limit, usage, errorCode, etc.)
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Subscription? Subscription { get; set; }
    
    // Helper property for metadata (not persisted, calculated from MetadataJson)
    public Dictionary<string, object> Metadata { get; set; } = new();
}

