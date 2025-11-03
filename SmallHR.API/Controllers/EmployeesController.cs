using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.API.Helpers;
using SmallHR.Core.DTOs;
using SmallHR.Core.DTOs.Employee;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : BaseApiController
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
        : base(logger)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// Get all employees (deprecated - use /search endpoint instead)
    /// </summary>
    [HttpGet]
    [AuthorizeHR]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
    {
        return await HandleCollectionResultAsync(
            () => _employeeService.GetAllEmployeesAsync(),
            "getting employees"
        );
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
    [AuthorizeHR]
    public async Task<ActionResult<PagedResponse<EmployeeDto>>> SearchEmployees(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? department = null,
        [FromQuery] string? position = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "FirstName",
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] string? tenantId = null)
    {
        try
        {
            // Normalize pagination
            (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize, 10, 100);

            // Check if user is SuperAdmin - only SuperAdmin can filter by tenantId
            // Use centralized permission service (follows Open/Closed Principle)
            // For SuperAdmin:
            // - If tenantId is provided, use it to filter by specific tenant
            // - If tenantId is null/empty, pass empty string to indicate "show all tenants"
            // For non-SuperAdmin:
            // - Always set tenantId to null (will use normal query filters)
            string? tenantIdForRequest = HttpContext.RequestServices
                .GetRequiredService<ITenantFilterService>()
                .ResolveTenantIdForRequest(IsSuperAdmin, tenantId);

            var request = new EmployeeSearchRequest
            {
                SearchTerm = searchTerm,
                Department = department,
                Position = position,
                IsActive = isActive,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDirection = sortDirection,
                TenantId = tenantIdForRequest
            };

            var result = await _employeeService.SearchEmployeesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while searching employees: {Error}", ex.Message);
            Logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            if (ex.InnerException != null)
            {
                Logger.LogError(ex.InnerException, "Inner exception: {InnerError}", ex.InnerException.Message);
            }
            return StatusCode(500, new { message = $"An error occurred while searching employees: {ex.Message}", detail = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// Search employees with pagination, filtering, and sorting (POST body)
    /// </summary>
    [HttpPost("search")]
    [AuthorizeHR]
    public async Task<ActionResult<PagedResponse<EmployeeDto>>> SearchEmployeesPost([FromBody] EmployeeSearchRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Normalize pagination
            (request.PageNumber, request.PageSize) = PaginationHelper.Normalize(request.PageNumber, request.PageSize, 10, 100);

            // Check if user is SuperAdmin - only SuperAdmin can filter by tenantId
            // Use centralized permission service (follows Open/Closed Principle)
            // For SuperAdmin:
            // - If TenantId is provided in request, use it to filter by specific tenant
            // - If TenantId is null/empty, set to empty string to indicate "show all tenants"
            // For non-SuperAdmin:
            // - Always set TenantId to null (will use normal query filters)
            request.TenantId = HttpContext.RequestServices
                .GetRequiredService<ITenantFilterService>()
                .ResolveTenantIdForRequest(IsSuperAdmin, request.TenantId);

            var result = await _employeeService.SearchEmployeesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while searching employees");
            return StatusCode(500, new { message = "An error occurred while searching employees" });
        }
    }

    /// <summary>
    /// Get employee by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
    {
        return await HandleServiceResultOrNotFoundAsync(
            () => _employeeService.GetEmployeeByIdAsync(id),
            $"getting employee with ID {id}",
            "Employee"
        );
    }

    /// <summary>
    /// Get employee by employee ID
    /// </summary>
    [HttpGet("by-employee-id/{employeeId}")]
    public async Task<ActionResult<EmployeeDto>> GetEmployeeByEmployeeId(string employeeId)
    {
        return await HandleServiceResultOrNotFoundAsync(
            () => _employeeService.GetEmployeeByEmployeeIdAsync(employeeId),
            $"getting employee with employee ID {employeeId}",
            "Employee"
        );
    }

    /// <summary>
    /// Create new employee
    /// </summary>
    [HttpPost]
    [AuthorizeHR]
    public async Task<ActionResult<EmployeeDto>> CreateEmployee(CreateEmployeeDto createEmployeeDto)
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

        return await HandleCreateResultAsync(
            () => _employeeService.CreateEmployeeAsync(createEmployeeDto),
            nameof(GetEmployee),
            emp => emp.Id,
            "creating employee"
        );
    }

    /// <summary>
    /// Update employee
    /// </summary>
    [HttpPut("{id}")]
    [AuthorizeHR]
    public async Task<ActionResult<EmployeeDto>> UpdateEmployee(int id, UpdateEmployeeDto updateEmployeeDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleUpdateResultAsync(
            () => _employeeService.EmployeeExistsAsync(id),
            () => _employeeService.UpdateEmployeeAsync(id, updateEmployeeDto),
            id,
            "updating employee",
            "Employee"
        );
    }

    /// <summary>
    /// Delete employee
    /// </summary>
    [HttpDelete("{id}")]
    [AuthorizeAdmin]
    public async Task<ActionResult> DeleteEmployee(int id)
    {
        return await HandleDeleteResultAsync(
            () => _employeeService.EmployeeExistsAsync(id),
            () => _employeeService.DeleteEmployeeAsync(id),
            id,
            "deleting employee",
            "Employee"
        );
    }

    /// <summary>
    /// Get employees by department
    /// </summary>
    [HttpGet("by-department/{department}")]
    [AuthorizeHR]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployeesByDepartment(string department)
    {
        return await HandleCollectionResultAsync(
            () => _employeeService.GetEmployeesByDepartmentAsync(department),
            $"getting employees by department {department}"
        );
    }

    /// <summary>
    /// Get active employees
    /// </summary>
    [HttpGet("active")]
    [AuthorizeHR]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetActiveEmployees()
    {
        return await HandleCollectionResultAsync(
            () => _employeeService.GetActiveEmployeesAsync(),
            "getting active employees"
        );
    }

    /// <summary>
    /// Get inactive employees
    /// </summary>
    [HttpGet("inactive")]
    [AuthorizeHR]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetInactiveEmployees()
    {
        return await HandleCollectionResultAsync(
            () => _employeeService.GetInactiveEmployeesAsync(),
            "getting inactive employees"
        );
    }
}
