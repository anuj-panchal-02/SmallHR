using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Repositories;

public class AttendanceRepository : GenericRepository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Attendance>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _dbSet
            .Where(a => a.EmployeeId == employeeId)
            .Include(a => a.Employee)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Attendance>> GetByDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate)
            .Include(a => a.Employee)
            .OrderBy(a => a.Date)
            .ToListAsync();
    }

    public async Task<Attendance?> GetByEmployeeAndDateAsync(int employeeId, DateTime date)
    {
        return await _dbSet
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == date.Date);
    }

    public async Task<IEnumerable<Attendance>> GetByDateAsync(DateTime date)
    {
        return await _dbSet
            .Where(a => a.Date.Date == date.Date)
            .Include(a => a.Employee)
            .ToListAsync();
    }

    public async Task<IEnumerable<Attendance>> GetByMonthAsync(int employeeId, int year, int month)
    {
        return await _dbSet
            .Where(a => a.EmployeeId == employeeId && a.Date.Year == year && a.Date.Month == month)
            .Include(a => a.Employee)
            .OrderBy(a => a.Date)
            .ToListAsync();
    }

    public async Task<TimeSpan> GetTotalHoursAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        var attendances = await _dbSet
            .Where(a => a.EmployeeId == employeeId 
                       && a.Date >= startDate 
                       && a.Date <= endDate 
                       && a.TotalHours.HasValue)
            .ToListAsync();

        return TimeSpan.FromTicks(attendances.Sum(a => a.TotalHours!.Value.Ticks));
    }

    public async Task<TimeSpan> GetOvertimeHoursAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        var attendances = await _dbSet
            .Where(a => a.EmployeeId == employeeId 
                       && a.Date >= startDate 
                       && a.Date <= endDate 
                       && a.OvertimeHours.HasValue)
            .ToListAsync();

        return TimeSpan.FromTicks(attendances.Sum(a => a.OvertimeHours!.Value.Ticks));
    }

    public async Task<bool> HasClockInAsync(int employeeId, DateTime date)
    {
        return await _dbSet.AnyAsync(a => a.EmployeeId == employeeId 
                                        && a.Date.Date == date.Date 
                                        && a.ClockInTime.HasValue);
    }

    public async Task<bool> HasClockOutAsync(int employeeId, DateTime date)
    {
        return await _dbSet.AnyAsync(a => a.EmployeeId == employeeId 
                                        && a.Date.Date == date.Date 
                                        && a.ClockOutTime.HasValue);
    }
}
