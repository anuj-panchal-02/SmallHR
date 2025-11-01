using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.Core.DTOs.Attendance;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all attendance records
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAttendance()
    {
        try
        {
            var attendance = await _attendanceService.GetAllAttendanceAsync();
            return Ok(attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting attendance records");
            return StatusCode(500, new { message = "An error occurred while getting attendance records" });
        }
    }

    /// <summary>
    /// Get attendance record by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AttendanceDto>> GetAttendance(int id)
    {
        try
        {
            var attendance = await _attendanceService.GetAttendanceByIdAsync(id);
            if (attendance == null)
            {
                return NotFound();
            }

            return Ok(attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting attendance record with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while getting attendance record" });
        }
    }

    /// <summary>
    /// Get attendance records by employee ID
    /// </summary>
    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAttendanceByEmployee(int employeeId)
    {
        try
        {
            var attendance = await _attendanceService.GetAttendanceByEmployeeIdAsync(employeeId);
            return Ok(attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting attendance records for employee ID: {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "An error occurred while getting attendance records" });
        }
    }

    /// <summary>
    /// Create new attendance record
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<AttendanceDto>> CreateAttendance(CreateAttendanceDto createAttendanceDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var attendance = await _attendanceService.CreateAttendanceAsync(createAttendanceDto);
            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating attendance record");
            return StatusCode(500, new { message = "An error occurred while creating attendance record" });
        }
    }

    /// <summary>
    /// Update attendance record
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<AttendanceDto>> UpdateAttendance(int id, UpdateAttendanceDto updateAttendanceDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _attendanceService.AttendanceExistsAsync(id))
            {
                return NotFound();
            }

            var attendance = await _attendanceService.UpdateAttendanceAsync(id, updateAttendanceDto);
            if (attendance == null)
            {
                return NotFound();
            }

            return Ok(attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating attendance record with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating attendance record" });
        }
    }

    /// <summary>
    /// Delete attendance record
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> DeleteAttendance(int id)
    {
        try
        {
            if (!await _attendanceService.AttendanceExistsAsync(id))
            {
                return NotFound();
            }

            var result = await _attendanceService.DeleteAttendanceAsync(id);
            if (!result)
            {
                return BadRequest();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting attendance record with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting attendance record" });
        }
    }

    /// <summary>
    /// Clock in
    /// </summary>
    [HttpPost("clock-in")]
    public async Task<ActionResult<AttendanceDto>> ClockIn(ClockInDto clockInDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var attendance = await _attendanceService.ClockInAsync(clockInDto);
            return Ok(attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during clock in for employee ID: {EmployeeId}", clockInDto.EmployeeId);
            return StatusCode(500, new { message = "An error occurred during clock in" });
        }
    }

    /// <summary>
    /// Clock out
    /// </summary>
    [HttpPost("clock-out")]
    public async Task<ActionResult<AttendanceDto>> ClockOut(ClockOutDto clockOutDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var attendance = await _attendanceService.ClockOutAsync(clockOutDto);
            if (attendance == null)
            {
                return BadRequest(new { message = "No clock in record found for today" });
            }

            return Ok(attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during clock out for employee ID: {EmployeeId}", clockOutDto.EmployeeId);
            return StatusCode(500, new { message = "An error occurred during clock out" });
        }
    }

    /// <summary>
    /// Get attendance records by date range
    /// </summary>
    [HttpGet("employee/{employeeId}/date-range")]
    public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAttendanceByDateRange(
        int employeeId, 
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        try
        {
            var attendance = await _attendanceService.GetAttendanceByDateRangeAsync(employeeId, startDate, endDate);
            return Ok(attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting attendance records by date range for employee ID: {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "An error occurred while getting attendance records by date range" });
        }
    }

    /// <summary>
    /// Get attendance record by employee and date
    /// </summary>
    [HttpGet("employee/{employeeId}/date")]
    public async Task<ActionResult<AttendanceDto>> GetAttendanceByEmployeeAndDate(int employeeId, [FromQuery] DateTime date)
    {
        try
        {
            var attendance = await _attendanceService.GetAttendanceByEmployeeAndDateAsync(employeeId, date);
            if (attendance == null)
            {
                return NotFound();
            }

            return Ok(attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting attendance record for employee ID: {EmployeeId} and date: {Date}", employeeId, date);
            return StatusCode(500, new { message = "An error occurred while getting attendance record" });
        }
    }

    /// <summary>
    /// Get attendance records by month
    /// </summary>
    [HttpGet("employee/{employeeId}/month")]
    public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAttendanceByMonth(
        int employeeId, 
        [FromQuery] int year, 
        [FromQuery] int month)
    {
        try
        {
            var attendance = await _attendanceService.GetAttendanceByMonthAsync(employeeId, year, month);
            return Ok(attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting attendance records by month for employee ID: {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "An error occurred while getting attendance records by month" });
        }
    }

    /// <summary>
    /// Get total hours for an employee
    /// </summary>
    [HttpGet("employee/{employeeId}/total-hours")]
    public async Task<ActionResult<TimeSpan>> GetTotalHours(int employeeId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var totalHours = await _attendanceService.GetTotalHoursAsync(employeeId, startDate, endDate);
            return Ok(new { totalHours });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting total hours for employee ID: {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "An error occurred while getting total hours" });
        }
    }

    /// <summary>
    /// Get overtime hours for an employee
    /// </summary>
    [HttpGet("employee/{employeeId}/overtime-hours")]
    public async Task<ActionResult<TimeSpan>> GetOvertimeHours(int employeeId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var overtimeHours = await _attendanceService.GetOvertimeHoursAsync(employeeId, startDate, endDate);
            return Ok(new { overtimeHours });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting overtime hours for employee ID: {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "An error occurred while getting overtime hours" });
        }
    }

    /// <summary>
    /// Check if employee has clocked in today
    /// </summary>
    [HttpGet("employee/{employeeId}/has-clock-in")]
    public async Task<ActionResult<bool>> HasClockIn(int employeeId, [FromQuery] DateTime date)
    {
        try
        {
            var hasClockIn = await _attendanceService.HasClockInAsync(employeeId, date);
            return Ok(new { hasClockIn });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking clock in status for employee ID: {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "An error occurred while checking clock in status" });
        }
    }

    /// <summary>
    /// Check if employee has clocked out today
    /// </summary>
    [HttpGet("employee/{employeeId}/has-clock-out")]
    public async Task<ActionResult<bool>> HasClockOut(int employeeId, [FromQuery] DateTime date)
    {
        try
        {
            var hasClockOut = await _attendanceService.HasClockOutAsync(employeeId, date);
            return Ok(new { hasClockOut });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking clock out status for employee ID: {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "An error occurred while checking clock out status" });
        }
    }
}
