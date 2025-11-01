using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.Entities;

public class Attendance : BaseEntity
{
    public required string TenantId { get; set; }
    [Required]
    public int EmployeeId { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    public DateTime? ClockInTime { get; set; }
    
    public DateTime? ClockOutTime { get; set; }
    
    public TimeSpan? TotalHours { get; set; }
    
    public TimeSpan? OvertimeHours { get; set; }
    
    [Required]
    public string Status { get; set; } = "Present"; // Present, Absent, Late, Half Day
    
    public string? Notes { get; set; }
    
    public bool IsHoliday { get; set; } = false;
    
    public bool IsWeekend { get; set; } = false;
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
}
