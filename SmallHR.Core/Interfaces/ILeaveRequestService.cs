using SmallHR.Core.DTOs.LeaveRequest;

namespace SmallHR.Core.Interfaces;

public interface ILeaveRequestService : IService
{
    Task<IEnumerable<LeaveRequestDto>> GetAllLeaveRequestsAsync();
    Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id);
    Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByEmployeeIdAsync(int employeeId);
    Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequestDto createLeaveRequestDto);
    Task<LeaveRequestDto?> UpdateLeaveRequestAsync(int id, UpdateLeaveRequestDto updateLeaveRequestDto);
    Task<LeaveRequestDto?> ApproveLeaveRequestAsync(int id, ApproveLeaveRequestDto approveLeaveRequestDto, string approvedBy);
    Task<bool> DeleteLeaveRequestAsync(int id);
    Task<bool> LeaveRequestExistsAsync(int id);
    Task<IEnumerable<LeaveRequestDto>> GetPendingLeaveRequestsAsync();
    Task<IEnumerable<LeaveRequestDto>> GetApprovedLeaveRequestsAsync();
    Task<IEnumerable<LeaveRequestDto>> GetRejectedLeaveRequestsAsync();
    Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<int> GetTotalLeaveDaysAsync(int employeeId, string leaveType, int year);
}
