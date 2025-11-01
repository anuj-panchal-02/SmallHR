using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.DTOs.LeaveRequest;

public class LeaveRequestDto : BaseDto
{
    public int EmployeeId { get; set; }
    
    public string EmployeeName { get; set; } = string.Empty;
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public string LeaveType { get; set; } = string.Empty;
    
    public string Reason { get; set; } = string.Empty;
    
    public string? Comments { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public string? ApprovedBy { get; set; }
    
    public DateTime? ApprovedAt { get; set; }
    
    public string? RejectionReason { get; set; }
    
    public int TotalDays { get; set; }
}

public class CreateLeaveRequestDto
{
    [Required]
    public int EmployeeId { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    [Required]
    public string LeaveType { get; set; } = string.Empty;
    
    [Required]
    public string Reason { get; set; } = string.Empty;
    
    public string? Comments { get; set; }
}

public class UpdateLeaveRequestDto
{
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    [Required]
    public string LeaveType { get; set; } = string.Empty;
    
    [Required]
    public string Reason { get; set; } = string.Empty;
    
    public string? Comments { get; set; }
}

public class ApproveLeaveRequestDto
{
    [Required]
    public string Status { get; set; } = string.Empty; // Approved, Rejected
    
    public string? RejectionReason { get; set; }
}
