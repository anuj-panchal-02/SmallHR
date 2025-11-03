using SmallHR.Core.Entities;

namespace SmallHR.Core.Interfaces;

public interface ILeaveRequestRepository : IGenericRepository<LeaveRequest>
{
    Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<LeaveRequest>> GetByStatusAsync(string status);
    Task<IEnumerable<LeaveRequest>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync();
    Task<IEnumerable<LeaveRequest>> GetApprovedRequestsAsync();
    Task<IEnumerable<LeaveRequest>> GetRejectedRequestsAsync();
    Task<int> GetTotalLeaveDaysAsync(int employeeId, string leaveType, int year);
    Task<IEnumerable<LeaveRequest>> GetAllAsync(string? tenantId = null);
}
