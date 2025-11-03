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

    public new async Task<IEnumerable<LeaveRequest>> GetAllAsync(string? tenantId = null)
    {
        var query = _dbSet.AsQueryable();

        // SuperAdmin filtering logic:
        // - If tenantId is provided (non-empty), ignore query filters and filter by that specific tenant
        // - If tenantId is empty string (""), it means SuperAdmin wants to see ALL tenants - ignore query filters
        // - If tenantId is null, use normal query filters (regular user or tenantId not provided)
        
        if (tenantId != null)
        {
            // tenantId parameter was provided (SuperAdmin context)
            query = query.IgnoreQueryFilters();
            
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                // Filter to specific tenant
                query = query.Where(lr => lr.TenantId == tenantId);
            }
            // else tenantId is empty string - show all tenants (no additional filter)
        }
        // else: tenantId is null - use normal query filters (regular user)

        // Filter out deleted leave requests
        query = query.Where(lr => !lr.IsDeleted);

        return await query.Include(lr => lr.Employee).ToListAsync();
    }
}
