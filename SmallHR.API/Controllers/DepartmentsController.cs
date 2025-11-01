using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.Core.DTOs.Department;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin,HR")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(IDepartmentService departmentService, ILogger<DepartmentsController> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all departments
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
    {
        try
        {
            var departments = await _departmentService.GetAllDepartmentsAsync();
            return Ok(departments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting departments");
            return StatusCode(500, new { message = "An error occurred while getting departments" });
        }
    }

    /// <summary>
    /// Get department by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetDepartment(int id)
    {
        try
        {
            var department = await _departmentService.GetDepartmentByIdAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            return Ok(department);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting department with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while getting department" });
        }
    }

    /// <summary>
    /// Get department names (for dropdowns)
    /// </summary>
    [HttpGet("names")]
    public async Task<ActionResult<IEnumerable<string>>> GetDepartmentNames()
    {
        try
        {
            var names = await _departmentService.GetDepartmentNamesAsync();
            return Ok(names);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting department names");
            return StatusCode(500, new { message = "An error occurred while getting department names" });
        }
    }

    /// <summary>
    /// Create new department
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment([FromBody] CreateDepartmentDto createDepartmentDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var department = await _departmentService.CreateDepartmentAsync(createDepartmentDto);
            return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, department);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating department");
            return StatusCode(500, new { message = "An error occurred while creating department" });
        }
    }

    /// <summary>
    /// Update department
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<DepartmentDto>> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto updateDepartmentDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _departmentService.DepartmentExistsAsync(id))
            {
                return NotFound();
            }

            var department = await _departmentService.UpdateDepartmentAsync(id, updateDepartmentDto);
            if (department == null)
            {
                return NotFound();
            }

            return Ok(department);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating department with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating department" });
        }
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
            _logger.LogError(ex, "An error occurred while assigning head of department. Department ID: {Id}, Employee ID: {EmployeeId}", id, employeeId);
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
            _logger.LogError(ex, "An error occurred while removing head of department. Department ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while removing head of department" });
        }
    }

    /// <summary>
    /// Delete department
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> DeleteDepartment(int id)
    {
        try
        {
            if (!await _departmentService.DepartmentExistsAsync(id))
            {
                return NotFound();
            }

            var result = await _departmentService.DeleteDepartmentAsync(id);
            if (!result)
            {
                return BadRequest();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting department with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting department" });
        }
    }
}

