using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.Entities;

public class LeaveRequest : BaseEntity
{
    public required string TenantId { get; set; }
    [Required]
    public int EmployeeId { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    [Required]
    public string LeaveType { get; set; } = string.Empty; // Annual, Sick, Personal, etc.
    
    [Required]
    public string Reason { get; set; } = string.Empty;
    
    public string? Comments { get; set; }
    
    [Required]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    
    public string? ApprovedBy { get; set; }
    
    public DateTime? ApprovedAt { get; set; }
    
    public string? RejectionReason { get; set; }
    
    public int TotalDays { get; set; }
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
}
