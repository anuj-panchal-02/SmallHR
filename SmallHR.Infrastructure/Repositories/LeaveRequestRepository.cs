using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Repositories;

public class LeaveRequestRepository : GenericRepository<LeaveRequest>, ILeaveRequestRepository
{
    public LeaveRequestRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _dbSet
            .Where(lr => lr.EmployeeId == employeeId)
            .Include(lr => lr.Employee)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaveRequest>> GetByStatusAsync(string status)
    {
        return await _dbSet
            .Where(lr => lr.Status == status)
            .Include(lr => lr.Employee)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaveRequest>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(lr => lr.StartDate >= startDate && lr.EndDate <= endDate)
            .Include(lr => lr.Employee)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync()
    {
        return await GetByStatusAsync("Pending");
    }

    public async Task<IEnumerable<LeaveRequest>> GetApprovedRequestsAsync()
    {
        return await GetByStatusAsync("Approved");
    }

    public async Task<IEnumerable<LeaveRequest>> GetRejectedRequestsAsync()
    {
        return await GetByStatusAsync("Rejected");
    }

    public async Task<int> GetTotalLeaveDaysAsync(int employeeId, string leaveType, int year)
    {
        return await _dbSet
            .Where(lr => lr.EmployeeId == employeeId 
                        && lr.LeaveType == leaveType 
                        && lr.Status == "Approved"
                        && lr.StartDate.Year == year)
            .SumAsync(lr => lr.TotalDays);
    }
}
