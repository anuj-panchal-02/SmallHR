using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.Core.DTOs;
using SmallHR.Core.DTOs.Employee;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all employees (deprecated - use /search endpoint instead)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
    {
        try
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            return Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting employees");
            return StatusCode(500, new { message = "An error occurred while getting employees" });
        }
    }

    /// <summary>
    /// Search employees with pagination, filtering, and sorting (Query Parameters)
    /// </summary>
    /// <param name="searchTerm">Search across name, email, and employee ID</param>
    /// <param name="department">Filter by department</param>
    /// <param name="position">Filter by position</param>
    /// <param name="isActive">Filter by active status (true/false/null for all)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <param name="sortBy">Sort field: FirstName, LastName, Email, EmployeeId, Department, Position, HireDate, Salary, CreatedAt (default: FirstName)</param>
    /// <param name="sortDirection">Sort direction: asc or desc (default: asc)</param>
    [HttpGet("search")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<PagedResponse<EmployeeDto>>> SearchEmployees(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? department = null,
        [FromQuery] string? position = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "FirstName",
        [FromQuery] string? sortDirection = "asc")
    {
        try
        {
            // Validate page size
            if (pageSize > 100) pageSize = 100;
            if (pageSize < 1) pageSize = 10;
            
            // Validate page number
            if (pageNumber < 1) pageNumber = 1;

            var request = new EmployeeSearchRequest
            {
                SearchTerm = searchTerm,
                Department = department,
                Position = position,
                IsActive = isActive,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            var result = await _employeeService.SearchEmployeesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while searching employees: {Error}", ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            if (ex.InnerException != null)
            {
                _logger.LogError(ex.InnerException, "Inner exception: {InnerError}", ex.InnerException.Message);
            }
            return StatusCode(500, new { message = $"An error occurred while searching employees: {ex.Message}", detail = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// Search employees with pagination, filtering, and sorting (POST body)
    /// </summary>
    [HttpPost("search")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<PagedResponse<EmployeeDto>>> SearchEmployeesPost([FromBody] EmployeeSearchRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate page size
            if (request.PageSize > 100) request.PageSize = 100;
            if (request.PageSize < 1) request.PageSize = 10;
            
            // Validate page number
            if (request.PageNumber < 1) request.PageNumber = 1;

            var result = await _employeeService.SearchEmployeesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while searching employees");
            return StatusCode(500, new { message = "An error occurred while searching employees" });
        }
    }

    /// <summary>
    /// Get employee by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
    {
        try
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting employee with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while getting employee" });
        }
    }

    /// <summary>
    /// Get employee by employee ID
    /// </summary>
    [HttpGet("by-employee-id/{employeeId}")]
    public async Task<ActionResult<EmployeeDto>> GetEmployeeByEmployeeId(string employeeId)
    {
        try
        {
            var employee = await _employeeService.GetEmployeeByEmployeeIdAsync(employeeId);
            if (employee == null)
            {
                return NotFound();
            }

            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting employee with employee ID: {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "An error occurred while getting employee" });
        }
    }

    /// <summary>
    /// Create new employee
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<EmployeeDto>> CreateEmployee(CreateEmployeeDto createEmployeeDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if employee ID already exists
            if (await _employeeService.EmployeeIdExistsAsync(createEmployeeDto.EmployeeId))
            {
                return BadRequest(new { message = "Employee ID already exists" });
            }

            // Check if email already exists as an employee
            // Note: If email exists as a user but not as employee, we'll link them during employee creation
            if (await _employeeService.EmailExistsAsync(createEmployeeDto.Email))
            {
                return BadRequest(new { message = "Email already exists for another employee" });
            }

            var employee = await _employeeService.CreateEmployeeAsync(createEmployeeDto);
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating employee: {Message}", ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            
            // Return detailed error message to help diagnose user creation issues
            var errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += $" Inner: {ex.InnerException.Message}";
            }
            
            return StatusCode(500, new { 
                message = "An error occurred while creating employee",
                detail = errorMessage,
                errorType = ex.GetType().Name
            });
        }
    }

    /// <summary>
    /// Update employee
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<EmployeeDto>> UpdateEmployee(int id, UpdateEmployeeDto updateEmployeeDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _employeeService.EmployeeExistsAsync(id))
            {
                return NotFound();
            }

            var employee = await _employeeService.UpdateEmployeeAsync(id, updateEmployeeDto);
            if (employee == null)
            {
                return NotFound();
            }

            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating employee with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating employee" });
        }
    }

    /// <summary>
    /// Delete employee
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> DeleteEmployee(int id)
    {
        try
        {
            if (!await _employeeService.EmployeeExistsAsync(id))
            {
                return NotFound();
            }

            var result = await _employeeService.DeleteEmployeeAsync(id);
            if (!result)
            {
                return BadRequest();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting employee with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting employee" });
        }
    }

    /// <summary>
    /// Get employees by department
    /// </summary>
    [HttpGet("by-department/{department}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployeesByDepartment(string department)
    {
        try
        {
            var employees = await _employeeService.GetEmployeesByDepartmentAsync(department);
            return Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting employees by department: {Department}", department);
            return StatusCode(500, new { message = "An error occurred while getting employees by department" });
        }
    }

    /// <summary>
    /// Get active employees
    /// </summary>
    [HttpGet("active")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetActiveEmployees()
    {
        try
        {
            var employees = await _employeeService.GetActiveEmployeesAsync();
            return Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting active employees");
            return StatusCode(500, new { message = "An error occurred while getting active employees" });
        }
    }

    /// <summary>
    /// Get inactive employees
    /// </summary>
    [HttpGet("inactive")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetInactiveEmployees()
    {
        try
        {
            var employees = await _employeeService.GetInactiveEmployeesAsync();
            return Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting inactive employees");
            return StatusCode(500, new { message = "An error occurred while getting inactive employees" });
        }
    }
}
