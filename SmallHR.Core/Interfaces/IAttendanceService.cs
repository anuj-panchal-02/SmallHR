using SmallHR.Core.DTOs.Attendance;

namespace SmallHR.Core.Interfaces;

public interface IAttendanceService : IService
{
    Task<IEnumerable<AttendanceDto>> GetAllAttendanceAsync();
    Task<AttendanceDto?> GetAttendanceByIdAsync(int id);
    Task<IEnumerable<AttendanceDto>> GetAttendanceByEmployeeIdAsync(int employeeId);
    Task<AttendanceDto> CreateAttendanceAsync(CreateAttendanceDto createAttendanceDto);
    Task<AttendanceDto?> UpdateAttendanceAsync(int id, UpdateAttendanceDto updateAttendanceDto);
    Task<bool> DeleteAttendanceAsync(int id);
    Task<bool> AttendanceExistsAsync(int id);
    Task<IEnumerable<AttendanceDto>> GetAttendanceByDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task<AttendanceDto?> GetAttendanceByEmployeeAndDateAsync(int employeeId, DateTime date);
    Task<IEnumerable<AttendanceDto>> GetAttendanceByDateAsync(DateTime date);
    Task<IEnumerable<AttendanceDto>> GetAttendanceByMonthAsync(int employeeId, int year, int month);
    Task<AttendanceDto> ClockInAsync(ClockInDto clockInDto);
    Task<AttendanceDto?> ClockOutAsync(ClockOutDto clockOutDto);
    Task<TimeSpan> GetTotalHoursAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task<TimeSpan> GetOvertimeHoursAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task<bool> HasClockInAsync(int employeeId, DateTime date);
    Task<bool> HasClockOutAsync(int employeeId, DateTime date);
}
