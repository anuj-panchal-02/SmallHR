using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.DTOs.Attendance;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : BaseApiController
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
        : base(logger)
    {
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// Get all attendance records
    /// </summary>
    [HttpGet]
    [AuthorizeHR]
    public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAttendance([FromQuery] string? tenantId = null)
    {
        var tenantIdForRequest = HttpContext.RequestServices
            .GetRequiredService<ITenantFilterService>()
            .ResolveTenantIdForRequest(IsSuperAdmin, tenantId);

        return await HandleCollectionResultAsync(
            () => _attendanceService.GetAllAttendanceAsync(tenantIdForRequest),
            "getting attendance records"
        );
    }

    /// <summary>
    /// Get attendance record by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AttendanceDto>> GetAttendance(int id)
    {
        return await HandleServiceResultOrNotFoundAsync(
            () => _attendanceService.GetAttendanceByIdAsync(id),
            $"getting attendance record with ID {id}",
            "Attendance"
        );
    }

    /// <summary>
    /// Get attendance records by employee ID
    /// </summary>
    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAttendanceByEmployee(int employeeId)
    {
        return await HandleCollectionResultAsync(
            () => _attendanceService.GetAttendanceByEmployeeIdAsync(employeeId),
            $"getting attendance records for employee ID {employeeId}"
        );
    }

    /// <summary>
    /// Create new attendance record
    /// </summary>
    [HttpPost]
    [AuthorizeHR]
    public async Task<ActionResult<AttendanceDto>> CreateAttendance(CreateAttendanceDto createAttendanceDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleCreateResultAsync(
            () => _attendanceService.CreateAttendanceAsync(createAttendanceDto),
            nameof(GetAttendance),
            att => att.Id,
            "creating attendance record"
        );
    }

    /// <summary>
    /// Update attendance record
    /// </summary>
    [HttpPut("{id}")]
    [AuthorizeHR]
    public async Task<ActionResult<AttendanceDto>> UpdateAttendance(int id, UpdateAttendanceDto updateAttendanceDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleUpdateResultAsync(
            () => _attendanceService.AttendanceExistsAsync(id),
            () => _attendanceService.UpdateAttendanceAsync(id, updateAttendanceDto),
            id,
            "updating attendance record",
            "Attendance"
        );
    }

    /// <summary>
    /// Delete attendance record
    /// </summary>
    [HttpDelete("{id}")]
    [AuthorizeAdmin]
    public async Task<ActionResult> DeleteAttendance(int id)
    {
        return await HandleDeleteResultAsync(
            () => _attendanceService.AttendanceExistsAsync(id),
            () => _attendanceService.DeleteAttendanceAsync(id),
            id,
            "deleting attendance record",
            "Attendance"
        );
    }

    /// <summary>
    /// Clock in
    /// </summary>
    [HttpPost("clock-in")]
    public async Task<ActionResult<AttendanceDto>> ClockIn(ClockInDto clockInDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleServiceResultAsync(
            () => _attendanceService.ClockInAsync(clockInDto),
            $"clocking in for employee ID {clockInDto.EmployeeId}"
        );
    }

    /// <summary>
    /// Clock out
    /// </summary>
    [HttpPost("clock-out")]
    public async Task<ActionResult<AttendanceDto>> ClockOut(ClockOutDto clockOutDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var attendance = await _attendanceService.ClockOutAsync(clockOutDto);
            if (attendance == null)
            {
                return CreateBadRequestResponse("No clock in record found for today");
            }

            return Ok(attendance);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred during clock out for employee ID: {EmployeeId}", clockOutDto.EmployeeId);
            return CreateErrorResponse("An error occurred during clock out", ex);
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
        return await HandleCollectionResultAsync(
            () => _attendanceService.GetAttendanceByDateRangeAsync(employeeId, startDate, endDate),
            $"getting attendance records by date range for employee ID {employeeId}"
        );
    }

    /// <summary>
    /// Get attendance record by employee and date
    /// </summary>
    [HttpGet("employee/{employeeId}/date")]
    public async Task<ActionResult<AttendanceDto>> GetAttendanceByEmployeeAndDate(int employeeId, [FromQuery] DateTime date)
    {
        return await HandleServiceResultOrNotFoundAsync(
            () => _attendanceService.GetAttendanceByEmployeeAndDateAsync(employeeId, date),
            $"getting attendance record for employee ID {employeeId} and date {date}",
            "Attendance"
        );
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
        return await HandleCollectionResultAsync(
            () => _attendanceService.GetAttendanceByMonthAsync(employeeId, year, month),
            $"getting attendance records by month for employee ID {employeeId}"
        );
    }

    /// <summary>
    /// Get total hours for an employee
    /// </summary>
    [HttpGet("employee/{employeeId}/total-hours")]
    public async Task<ActionResult<TimeSpan>> GetTotalHours(int employeeId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        return await HandleServiceResultAsync(
            async () => await _attendanceService.GetTotalHoursAsync(employeeId, startDate, endDate),
            $"getting total hours for employee ID {employeeId}"
        );
    }

    /// <summary>
    /// Get overtime hours for an employee
    /// </summary>
    [HttpGet("employee/{employeeId}/overtime-hours")]
    public async Task<ActionResult<TimeSpan>> GetOvertimeHours(int employeeId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        return await HandleServiceResultAsync(
            async () => await _attendanceService.GetOvertimeHoursAsync(employeeId, startDate, endDate),
            $"getting overtime hours for employee ID {employeeId}"
        );
    }

    /// <summary>
    /// Check if employee has clocked in today
    /// </summary>
    [HttpGet("employee/{employeeId}/has-clock-in")]
    public async Task<ActionResult<bool>> HasClockIn(int employeeId, [FromQuery] DateTime date)
    {
        return await HandleServiceResultAsync(
            async () => await _attendanceService.HasClockInAsync(employeeId, date),
            $"checking clock in status for employee ID {employeeId}"
        );
    }

    /// <summary>
    /// Check if employee has clocked out today
    /// </summary>
    [HttpGet("employee/{employeeId}/has-clock-out")]
    public async Task<ActionResult<bool>> HasClockOut(int employeeId, [FromQuery] DateTime date)
    {
        return await HandleServiceResultAsync(
            async () => await _attendanceService.HasClockOutAsync(employeeId, date),
            $"checking clock out status for employee ID {employeeId}"
        );
    }
}
