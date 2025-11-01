using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.Core.DTOs.LeaveRequest;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly ILogger<LeaveRequestsController> _logger;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService, ILogger<LeaveRequestsController> logger)
    {
        _leaveRequestService = leaveRequestService;
        _logger = logger;
    }

    /// <summary>
    /// Get all leave requests
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetLeaveRequests()
    {
        try
        {
            var leaveRequests = await _leaveRequestService.GetAllLeaveRequestsAsync();
            return Ok(leaveRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting leave requests");
            return StatusCode(500, new { message = "An error occurred while getting leave requests" });
        }
    }

    /// <summary>
    /// Get leave request by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LeaveRequestDto>> GetLeaveRequest(int id)
    {
        try
        {
            var leaveRequest = await _leaveRequestService.GetLeaveRequestByIdAsync(id);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            return Ok(leaveRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting leave request with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while getting leave request" });
        }
    }

    /// <summary>
    /// Get leave requests by employee ID
    /// </summary>
    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetLeaveRequestsByEmployee(int employeeId)
    {
        try
        {
            var leaveRequests = await _leaveRequestService.GetLeaveRequestsByEmployeeIdAsync(employeeId);
            return Ok(leaveRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting leave requests for employee ID: {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "An error occurred while getting leave requests" });
        }
    }

    /// <summary>
    /// Create new leave request
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> CreateLeaveRequest(CreateLeaveRequestDto createLeaveRequestDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var leaveRequest = await _leaveRequestService.CreateLeaveRequestAsync(createLeaveRequestDto);
            return CreatedAtAction(nameof(GetLeaveRequest), new { id = leaveRequest.Id }, leaveRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating leave request");
            return StatusCode(500, new { message = "An error occurred while creating leave request" });
        }
    }

    /// <summary>
    /// Update leave request
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<LeaveRequestDto>> UpdateLeaveRequest(int id, UpdateLeaveRequestDto updateLeaveRequestDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _leaveRequestService.LeaveRequestExistsAsync(id))
            {
                return NotFound();
            }

            var leaveRequest = await _leaveRequestService.UpdateLeaveRequestAsync(id, updateLeaveRequestDto);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            return Ok(leaveRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating leave request with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating leave request" });
        }
    }

    /// <summary>
    /// Approve or reject leave request
    /// </summary>
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<LeaveRequestDto>> ApproveLeaveRequest(int id, ApproveLeaveRequestDto approveLeaveRequestDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _leaveRequestService.LeaveRequestExistsAsync(id))
            {
                return NotFound();
            }

            var approvedBy = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Unknown";
            var leaveRequest = await _leaveRequestService.ApproveLeaveRequestAsync(id, approveLeaveRequestDto, approvedBy);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            return Ok(leaveRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while approving leave request with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while approving leave request" });
        }
    }

    /// <summary>
    /// Delete leave request
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteLeaveRequest(int id)
    {
        try
        {
            if (!await _leaveRequestService.LeaveRequestExistsAsync(id))
            {
                return NotFound();
            }

            var result = await _leaveRequestService.DeleteLeaveRequestAsync(id);
            if (!result)
            {
                return BadRequest();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting leave request with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting leave request" });
        }
    }

    /// <summary>
    /// Get pending leave requests
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetPendingLeaveRequests()
    {
        try
        {
            var leaveRequests = await _leaveRequestService.GetPendingLeaveRequestsAsync();
            return Ok(leaveRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting pending leave requests");
            return StatusCode(500, new { message = "An error occurred while getting pending leave requests" });
        }
    }

    /// <summary>
    /// Get approved leave requests
    /// </summary>
    [HttpGet("approved")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetApprovedLeaveRequests()
    {
        try
        {
            var leaveRequests = await _leaveRequestService.GetApprovedLeaveRequestsAsync();
            return Ok(leaveRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting approved leave requests");
            return StatusCode(500, new { message = "An error occurred while getting approved leave requests" });
        }
    }

    /// <summary>
    /// Get rejected leave requests
    /// </summary>
    [HttpGet("rejected")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetRejectedLeaveRequests()
    {
        try
        {
            var leaveRequests = await _leaveRequestService.GetRejectedLeaveRequestsAsync();
            return Ok(leaveRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting rejected leave requests");
            return StatusCode(500, new { message = "An error occurred while getting rejected leave requests" });
        }
    }

    /// <summary>
    /// Get leave requests by date range
    /// </summary>
    [HttpGet("by-date-range")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetLeaveRequestsByDateRange(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        try
        {
            var leaveRequests = await _leaveRequestService.GetLeaveRequestsByDateRangeAsync(startDate, endDate);
            return Ok(leaveRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting leave requests by date range");
            return StatusCode(500, new { message = "An error occurred while getting leave requests by date range" });
        }
    }

    /// <summary>
    /// Get total leave days for an employee
    /// </summary>
    [HttpGet("total-days/{employeeId}")]
    public async Task<ActionResult<int>> GetTotalLeaveDays(int employeeId, [FromQuery] string leaveType, [FromQuery] int year)
    {
        try
        {
            var totalDays = await _leaveRequestService.GetTotalLeaveDaysAsync(employeeId, leaveType, year);
            return Ok(new { totalDays });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting total leave days for employee ID: {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "An error occurred while getting total leave days" });
        }
    }
}
