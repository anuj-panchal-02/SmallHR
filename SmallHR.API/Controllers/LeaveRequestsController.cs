using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.DTOs.LeaveRequest;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveRequestsController : BaseApiController
{
    private readonly ILeaveRequestService _leaveRequestService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService, ILogger<LeaveRequestsController> logger)
        : base(logger)
    {
        _leaveRequestService = leaveRequestService;
    }

    /// <summary>
    /// Get all leave requests
    /// </summary>
    [HttpGet]
    [AuthorizeHR]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetLeaveRequests([FromQuery] string? tenantId = null)
    {
        var tenantIdForRequest = HttpContext.RequestServices
            .GetRequiredService<ITenantFilterService>()
            .ResolveTenantIdForRequest(IsSuperAdmin, tenantId);

        return await HandleCollectionResultAsync(
            () => _leaveRequestService.GetAllLeaveRequestsAsync(tenantIdForRequest),
            "getting leave requests"
        );
    }

    /// <summary>
    /// Get leave request by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LeaveRequestDto>> GetLeaveRequest(int id)
    {
        return await HandleServiceResultOrNotFoundAsync(
            () => _leaveRequestService.GetLeaveRequestByIdAsync(id),
            $"getting leave request with ID {id}",
            "LeaveRequest"
        );
    }

    /// <summary>
    /// Get leave requests by employee ID
    /// </summary>
    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetLeaveRequestsByEmployee(int employeeId)
    {
        return await HandleCollectionResultAsync(
            () => _leaveRequestService.GetLeaveRequestsByEmployeeIdAsync(employeeId),
            $"getting leave requests for employee ID {employeeId}"
        );
    }

    /// <summary>
    /// Create new leave request
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> CreateLeaveRequest(CreateLeaveRequestDto createLeaveRequestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleCreateResultAsync(
            () => _leaveRequestService.CreateLeaveRequestAsync(createLeaveRequestDto),
            nameof(GetLeaveRequest),
            lr => lr.Id,
            "creating leave request"
        );
    }

    /// <summary>
    /// Update leave request
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<LeaveRequestDto>> UpdateLeaveRequest(int id, UpdateLeaveRequestDto updateLeaveRequestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleUpdateResultAsync(
            () => _leaveRequestService.LeaveRequestExistsAsync(id),
            () => _leaveRequestService.UpdateLeaveRequestAsync(id, updateLeaveRequestDto),
            id,
            "updating leave request",
            "LeaveRequest"
        );
    }

    /// <summary>
    /// Approve or reject leave request
    /// </summary>
    [HttpPut("{id}/approve")]
    [AuthorizeHR]
    public async Task<ActionResult<LeaveRequestDto>> ApproveLeaveRequest(int id, ApproveLeaveRequestDto approveLeaveRequestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleUpdateResultAsync(
            () => _leaveRequestService.LeaveRequestExistsAsync(id),
            async () =>
            {
                var approvedBy = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Unknown";
                return await _leaveRequestService.ApproveLeaveRequestAsync(id, approveLeaveRequestDto, approvedBy);
            },
            id,
            "approving leave request",
            "LeaveRequest"
        );
    }

    /// <summary>
    /// Delete leave request
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteLeaveRequest(int id)
    {
        return await HandleDeleteResultAsync(
            () => _leaveRequestService.LeaveRequestExistsAsync(id),
            () => _leaveRequestService.DeleteLeaveRequestAsync(id),
            id,
            "deleting leave request",
            "LeaveRequest"
        );
    }

    /// <summary>
    /// Get pending leave requests
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetPendingLeaveRequests()
    {
        return await HandleCollectionResultAsync(
            () => _leaveRequestService.GetPendingLeaveRequestsAsync(),
            "getting pending leave requests"
        );
    }

    /// <summary>
    /// Get approved leave requests
    /// </summary>
    [HttpGet("approved")]
    [AuthorizeHR]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetApprovedLeaveRequests()
    {
        return await HandleCollectionResultAsync(
            () => _leaveRequestService.GetApprovedLeaveRequestsAsync(),
            "getting approved leave requests"
        );
    }

    /// <summary>
    /// Get rejected leave requests
    /// </summary>
    [HttpGet("rejected")]
    [AuthorizeHR]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetRejectedLeaveRequests()
    {
        return await HandleCollectionResultAsync(
            () => _leaveRequestService.GetRejectedLeaveRequestsAsync(),
            "getting rejected leave requests"
        );
    }

    /// <summary>
    /// Get leave requests by date range
    /// </summary>
    [HttpGet("by-date-range")]
    [AuthorizeHR]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetLeaveRequestsByDateRange(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        return await HandleCollectionResultAsync(
            () => _leaveRequestService.GetLeaveRequestsByDateRangeAsync(startDate, endDate),
            "getting leave requests by date range"
        );
    }

    /// <summary>
    /// Get total leave days for an employee
    /// </summary>
    [HttpGet("total-days/{employeeId}")]
    public async Task<ActionResult<int>> GetTotalLeaveDays(int employeeId, [FromQuery] string leaveType, [FromQuery] int year)
    {
        return await HandleServiceResultAsync(
            async () => await _leaveRequestService.GetTotalLeaveDaysAsync(employeeId, leaveType, year),
            $"getting total leave days for employee ID {employeeId}"
        );
    }
}
