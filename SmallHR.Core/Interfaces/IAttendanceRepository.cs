using SmallHR.Core.Entities;

namespace SmallHR.Core.Interfaces;

public interface IAttendanceRepository : IGenericRepository<Attendance>
{
    Task<IEnumerable<Attendance>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<Attendance>> GetByDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task<Attendance?> GetByEmployeeAndDateAsync(int employeeId, DateTime date);
    Task<IEnumerable<Attendance>> GetByDateAsync(DateTime date);
    Task<IEnumerable<Attendance>> GetByMonthAsync(int employeeId, int year, int month);
    Task<TimeSpan> GetTotalHoursAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task<TimeSpan> GetOvertimeHoursAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task<bool> HasClockInAsync(int employeeId, DateTime date);
    Task<bool> HasClockOutAsync(int employeeId, DateTime date);
}
