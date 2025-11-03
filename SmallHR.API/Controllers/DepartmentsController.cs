using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.DTOs.Department;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AuthorizeHR]
public class DepartmentsController : BaseApiController
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService, ILogger<DepartmentsController> logger)
        : base(logger)
    {
        _departmentService = departmentService;
    }

    /// <summary>
    /// Get all departments
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments([FromQuery] string? tenantId = null)
    {
        var tenantIdForRequest = HttpContext.RequestServices
            .GetRequiredService<ITenantFilterService>()
            .ResolveTenantIdForRequest(IsSuperAdmin, tenantId);

        return await HandleCollectionResultAsync(
            () => _departmentService.GetAllDepartmentsAsync(tenantIdForRequest),
            "getting departments"
        );
    }

    /// <summary>
    /// Get department by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetDepartment(int id)
    {
        return await HandleServiceResultOrNotFoundAsync(
            () => _departmentService.GetDepartmentByIdAsync(id),
            $"getting department with ID {id}",
            "Department"
        );
    }

    /// <summary>
    /// Get department names (for dropdowns)
    /// </summary>
    [HttpGet("names")]
    public async Task<ActionResult<IEnumerable<string>>> GetDepartmentNames()
    {
        return await HandleCollectionResultAsync(
            () => _departmentService.GetDepartmentNamesAsync(),
            "getting department names"
        );
    }

    /// <summary>
    /// Create new department
    /// </summary>
    [HttpPost]
    [AuthorizeAdmin]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment([FromBody] CreateDepartmentDto createDepartmentDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleCreateResultAsync(
            () => _departmentService.CreateDepartmentAsync(createDepartmentDto),
            nameof(GetDepartment),
            dept => dept.Id,
            "creating department"
        );
    }

    /// <summary>
    /// Update department
    /// </summary>
    [HttpPut("{id}")]
    [AuthorizeAdmin]
    public async Task<ActionResult<DepartmentDto>> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto updateDepartmentDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleUpdateResultAsync(
            () => _departmentService.DepartmentExistsAsync(id),
            () => _departmentService.UpdateDepartmentAsync(id, updateDepartmentDto),
            id,
            "updating department",
            "Department"
        );
    }

    /// <summary>
    /// Assign head of department by employee ID
    /// </summary>
    [HttpPut("{id}/assign-head/{employeeId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<DepartmentDto>> AssignHeadOfDepartment(int id, int employeeId)
    {
        try
        {
            if (!await _departmentService.DepartmentExistsAsync(id))
            {
                return NotFound(new { message = "Department not found" });
            }

            var department = await _departmentService.AssignHeadOfDepartmentAsync(id, employeeId);
            if (department == null)
            {
                return BadRequest(new { message = "Employee not found or not in this department" });
            }

            return Ok(department);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while assigning head of department. Department ID: {Id}, Employee ID: {EmployeeId}", id, employeeId);
            return StatusCode(500, new { message = "An error occurred while assigning head of department" });
        }
    }

    /// <summary>
    /// Remove head of department
    /// </summary>
    [HttpPut("{id}/remove-head")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<DepartmentDto>> RemoveHeadOfDepartment(int id)
    {
        try
        {
            if (!await _departmentService.DepartmentExistsAsync(id))
            {
                return NotFound(new { message = "Department not found" });
            }

            var department = await _departmentService.RemoveHeadOfDepartmentAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            return Ok(department);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while removing head of department. Department ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while removing head of department" });
        }
    }

    /// <summary>
    /// Delete department
    /// </summary>
    [HttpDelete("{id}")]
    [AuthorizeAdmin]
    public async Task<ActionResult> DeleteDepartment(int id)
    {
        return await HandleDeleteResultAsync(
            () => _departmentService.DepartmentExistsAsync(id),
            () => _departmentService.DeleteDepartmentAsync(id),
            id,
            "deleting department",
            "Department"
        );
    }
}

