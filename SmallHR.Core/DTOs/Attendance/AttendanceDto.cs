using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.DTOs.Attendance;

public class AttendanceDto : BaseDto
{
    public int EmployeeId { get; set; }
    
    public string EmployeeName { get; set; } = string.Empty;
    
    public DateTime Date { get; set; }
    
    public DateTime? ClockInTime { get; set; }
    
    public DateTime? ClockOutTime { get; set; }
    
    public TimeSpan? TotalHours { get; set; }
    
    public TimeSpan? OvertimeHours { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public string? Notes { get; set; }
    
    public bool IsHoliday { get; set; }
    
    public bool IsWeekend { get; set; }
}

public class CreateAttendanceDto
{
    [Required]
    public int EmployeeId { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    public DateTime? ClockInTime { get; set; }
    
    public DateTime? ClockOutTime { get; set; }
    
    public string? Notes { get; set; }
}

public class UpdateAttendanceDto
{
    public DateTime? ClockInTime { get; set; }
    
    public DateTime? ClockOutTime { get; set; }
    
    public string? Notes { get; set; }
}

public class ClockInDto
{
    [Required]
    public int EmployeeId { get; set; }
    
    public DateTime ClockInTime { get; set; } = DateTime.UtcNow;
}

public class ClockOutDto
{
    [Required]
    public int EmployeeId { get; set; }
    
    public DateTime ClockOutTime { get; set; } = DateTime.UtcNow;
}
