using AutoMapper;
using SmallHR.Core.DTOs.LeaveRequest;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMapper _mapper;
    private readonly ITenantProvider _tenantProvider;

    public LeaveRequestService(
        ILeaveRequestRepository leaveRequestRepository,
        IEmployeeRepository employeeRepository,
        IMapper mapper,
        ITenantProvider tenantProvider)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _employeeRepository = employeeRepository;
        _mapper = mapper;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetAllLeaveRequestsAsync(string? tenantId = null)
    {
        var leaveRequests = await _leaveRequestRepository.GetAllAsync(tenantId);
        return _mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
    }

    public async Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id);
        if (leaveRequest == null) return null;
        
        // Validate tenant ownership
        if (leaveRequest.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Leave request belongs to different tenant");
        }
        
        return _mapper.Map<LeaveRequestDto>(leaveRequest);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByEmployeeIdAsync(int employeeId)
    {
        var leaveRequests = await _leaveRequestRepository.GetByEmployeeIdAsync(employeeId);
        return _mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
    }

    public async Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequestDto createLeaveRequestDto)
    {
        var leaveRequest = _mapper.Map<LeaveRequest>(createLeaveRequestDto);
        leaveRequest.TotalDays = (int)(leaveRequest.EndDate - leaveRequest.StartDate).TotalDays + 1;
        
        await _leaveRequestRepository.AddAsync(leaveRequest);
        return _mapper.Map<LeaveRequestDto>(leaveRequest);
    }

    public async Task<LeaveRequestDto?> UpdateLeaveRequestAsync(int id, UpdateLeaveRequestDto updateLeaveRequestDto)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id);
        if (leaveRequest == null) return null;

        // Validate tenant ownership
        if (leaveRequest.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Leave request belongs to different tenant");
        }

        _mapper.Map(updateLeaveRequestDto, leaveRequest);
        leaveRequest.TotalDays = (int)(leaveRequest.EndDate - leaveRequest.StartDate).TotalDays + 1;
        leaveRequest.UpdatedAt = DateTime.UtcNow;
        
        await _leaveRequestRepository.UpdateAsync(leaveRequest);
        return _mapper.Map<LeaveRequestDto>(leaveRequest);
    }

    public async Task<LeaveRequestDto?> ApproveLeaveRequestAsync(int id, ApproveLeaveRequestDto approveLeaveRequestDto, string approvedBy)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id);
        if (leaveRequest == null) return null;

        // Validate tenant ownership
        if (leaveRequest.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Leave request belongs to different tenant");
        }

        leaveRequest.Status = approveLeaveRequestDto.Status;
        leaveRequest.ApprovedBy = approvedBy;
        leaveRequest.ApprovedAt = DateTime.UtcNow;
        
        if (approveLeaveRequestDto.Status == "Rejected")
        {
            leaveRequest.RejectionReason = approveLeaveRequestDto.RejectionReason;
        }
        
        leaveRequest.UpdatedAt = DateTime.UtcNow;
        await _leaveRequestRepository.UpdateAsync(leaveRequest);
        return _mapper.Map<LeaveRequestDto>(leaveRequest);
    }

    public async Task<bool> DeleteLeaveRequestAsync(int id)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id);
        if (leaveRequest == null) return false;

        // Validate tenant ownership
        if (leaveRequest.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Leave request belongs to different tenant");
        }

        leaveRequest.IsDeleted = true;
        leaveRequest.UpdatedAt = DateTime.UtcNow;
        await _leaveRequestRepository.UpdateAsync(leaveRequest);
        return true;
    }

    public async Task<bool> LeaveRequestExistsAsync(int id)
    {
        return await _leaveRequestRepository.ExistsAsync(lr => lr.Id == id);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetPendingLeaveRequestsAsync()
    {
        var leaveRequests = await _leaveRequestRepository.GetPendingRequestsAsync();
        return _mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetApprovedLeaveRequestsAsync()
    {
        var leaveRequests = await _leaveRequestRepository.GetApprovedRequestsAsync();
        return _mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetRejectedLeaveRequestsAsync()
    {
        var leaveRequests = await _leaveRequestRepository.GetRejectedRequestsAsync();
        return _mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var leaveRequests = await _leaveRequestRepository.GetByDateRangeAsync(startDate, endDate);
        return _mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
    }

    public async Task<int> GetTotalLeaveDaysAsync(int employeeId, string leaveType, int year)
    {
        return await _leaveRequestRepository.GetTotalLeaveDaysAsync(employeeId, leaveType, year);
    }
}
