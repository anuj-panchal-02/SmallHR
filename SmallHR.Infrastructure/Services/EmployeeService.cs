using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallHR.Core.DTOs;
using SmallHR.Core.DTOs.Employee;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMapper _mapper;
    private readonly ITenantProvider _tenantProvider;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<EmployeeService> _logger;
    private readonly ApplicationDbContext _context;

    public EmployeeService(
        IEmployeeRepository employeeRepository, 
        IMapper mapper, 
        ITenantProvider tenantProvider,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<EmployeeService> logger,
        ApplicationDbContext context)
    {
        _employeeRepository = employeeRepository;
        _mapper = mapper;
        _tenantProvider = tenantProvider;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _context = context;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync()
    {
        var employees = await _employeeRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null) return null;
        
        // Validate tenant ownership
        if (employee.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Employee belongs to different tenant");
        }
        
        return _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<EmployeeDto?> GetEmployeeByEmployeeIdAsync(string employeeId)
    {
        var employee = await _employeeRepository.GetByEmployeeIdAsync(employeeId);
        return employee == null ? null : _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto)
    {
        // Check subscription limit
        await CheckSubscriptionLimitAsync();
        
        var employee = _mapper.Map<Employee>(createEmployeeDto);
        bool userCreatedOrLinked = false;
        
        // Create user automatically for the employee
        try
        {
            // Check if user already exists with this email
            var existingUser = await _userManager.FindByEmailAsync(createEmployeeDto.Email);
            if (existingUser != null)
            {
                // Link existing user to employee
                employee.UserId = existingUser.Id;
                userCreatedOrLinked = true;
                _logger.LogInformation("Linked existing user {Email} (ID: {UserId}) to employee {EmployeeId}", 
                    createEmployeeDto.Email, existingUser.Id, createEmployeeDto.EmployeeId);
            }
            else
            {
                // Validate role exists
                var roleExists = await _roleManager.RoleExistsAsync(createEmployeeDto.Role);
                if (!roleExists)
                {
                    _logger.LogWarning("Role {Role} does not exist for employee {EmployeeId}, defaulting to Employee", createEmployeeDto.Role, createEmployeeDto.EmployeeId);
                    createEmployeeDto.Role = "Employee"; // Default to Employee role if invalid
                }

                // Generate a simple password
                var password = GenerateSimplePassword(createEmployeeDto.Email, createEmployeeDto.EmployeeId);
                _logger.LogInformation("Generated password for user {Email}: Length={Length}, Format=Welcome@{EmployeeId}123!", 
                    createEmployeeDto.Email, password.Length, createEmployeeDto.EmployeeId);
                _logger.LogWarning("USER PASSWORD INFO - Email: {Email}, EmployeeId: {EmployeeId}, Password: {Password}", 
                    createEmployeeDto.Email, createEmployeeDto.EmployeeId, password);

                // Create new user
                var user = new User
                {
                    UserName = createEmployeeDto.Email,
                    Email = createEmployeeDto.Email,
                    FirstName = createEmployeeDto.FirstName,
                    LastName = createEmployeeDto.LastName,
                    DateOfBirth = createEmployeeDto.DateOfBirth,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Attempting to create user in AspNetUsers: Email={Email}, UserName={UserName}, FirstName={FirstName}, LastName={LastName}, DateOfBirth={DateOfBirth}", 
                    user.Email, user.UserName, user.FirstName, user.LastName, user.DateOfBirth);

                var userResult = await _userManager.CreateAsync(user, password);
                
                _logger.LogInformation("UserManager.CreateAsync completed. Succeeded={Succeeded}, Errors={ErrorCount}", 
                    userResult.Succeeded, userResult.Errors?.Count() ?? 0);
                
                if (userResult.Succeeded)
                {
                    // UserManager.CreateAsync automatically saves to AspNetUsers table
                    // The user.Id is now populated with the database-generated ID
                    _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);
                    
                    // Verify user was actually saved by fetching it
                    var createdUser = await _userManager.FindByEmailAsync(createEmployeeDto.Email);
                    if (createdUser == null)
                    {
                        _logger.LogError("CRITICAL: User creation succeeded but user not found in database for email {Email}. UserId was: {UserId}", 
                            createEmployeeDto.Email, user.Id);
                        throw new InvalidOperationException($"User creation succeeded but user not found in database for {createEmployeeDto.Email}");
                    }
                    
                    _logger.LogInformation("Verified user exists in AspNetUsers table: Email={Email}, Id={UserId}", 
                        createdUser.Email, createdUser.Id);
                    
                    // Assign role to user (this adds entry to AspNetUserRoles table)
                    var roleResult = await _userManager.AddToRoleAsync(createdUser, createEmployeeDto.Role);
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to assign role {Role} to user {Email}: {Errors}", 
                            createEmployeeDto.Role, createEmployeeDto.Email, roleErrors);
                        throw new InvalidOperationException($"Failed to assign role {createEmployeeDto.Role} to user {createEmployeeDto.Email}: {roleErrors}");
                    }
                    
                    _logger.LogInformation("Role {Role} assigned to user {Email} (ID: {UserId}). User is in AspNetUsers table.", 
                        createEmployeeDto.Role, createEmployeeDto.Email, createdUser.Id);
                    
                    // Link user to employee by setting UserId foreign key
                    employee.UserId = createdUser.Id;
                    userCreatedOrLinked = true;
                    
                    _logger.LogInformation("SUCCESS: User {Email} (ID: {UserId}) created in AspNetUsers table with role {Role} for employee {EmployeeId}", 
                        createEmployeeDto.Email, createdUser.Id, createEmployeeDto.Role, createEmployeeDto.EmployeeId);
                    _logger.LogWarning("LOGIN CREDENTIALS - Email: {Email}, Password: Welcome@{EmployeeId}123!, Please use this password to login", 
                        createEmployeeDto.Email, createEmployeeDto.EmployeeId);
                }
                else
                {
                    var errors = string.Join(", ", userResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    _logger.LogError("Failed to create user for employee {EmployeeId}: {Errors}. User object: Email={Email}, UserName={UserName}, FirstName={FirstName}, LastName={LastName}", 
                        createEmployeeDto.EmployeeId, errors, createEmployeeDto.Email, user.UserName, user.FirstName, user.LastName);
                    
                    // Log password validation details if available
                    foreach (var error in userResult.Errors)
                    {
                        _logger.LogError("Identity error: Code={Code}, Description={Description}", error.Code, error.Description);
                    }
                    
                    // Throw exception to prevent employee creation if user creation fails
                    // This ensures data consistency - employee should not exist without user
                    throw new InvalidOperationException($"Failed to create user for employee {createEmployeeDto.EmployeeId}: {errors}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user for employee {EmployeeId}: {Message}. StackTrace: {StackTrace}", 
                createEmployeeDto.EmployeeId, ex.Message, ex.StackTrace);
            
            // Re-throw with more context to help debug - this prevents employee creation
            throw new InvalidOperationException($"Failed to create user for employee {createEmployeeDto.EmployeeId}: {ex.Message}", ex);
        }

        // Only create employee if user was successfully created or linked
        if (!userCreatedOrLinked || string.IsNullOrEmpty(employee.UserId))
        {
            _logger.LogError("CRITICAL: Attempting to create employee {EmployeeId} without a valid user. UserCreatedOrLinked: {UserCreatedOrLinked}, UserId: {UserId}", 
                createEmployeeDto.EmployeeId, userCreatedOrLinked, employee.UserId ?? "NULL");
            throw new InvalidOperationException($"Cannot create employee {createEmployeeDto.EmployeeId} without a valid user. User creation failed or was not completed.");
        }

        await _employeeRepository.AddAsync(employee);
        _logger.LogInformation("Employee {EmployeeId} created successfully with UserId: {UserId}", 
            createEmployeeDto.EmployeeId, employee.UserId);
        
        return _mapper.Map<EmployeeDto>(employee);
    }

    /// <summary>
    /// Generates a simple password based on employee ID
    /// Format: Welcome@{EmployeeId}123!
    /// Example: Welcome@EMP001123!
    /// Meets security requirements: 12+ chars, uppercase, lowercase, number, special char
    /// </summary>
    private string GenerateSimplePassword(string email, string employeeId)
    {
        // Simple password format: Welcome@{EmployeeId}123!
        // This format guarantees:
        // - Uppercase: "W" (from "Welcome")
        // - Lowercase: "elcome" (from "Welcome")
        // - Numbers: "123"
        // - Special characters: "@" and "!"
        // - Minimum 12 characters
        
        // Sanitize employeeId to ensure password validity (remove spaces, special chars that might break password)
        var sanitizedEmployeeId = employeeId
            .Replace(" ", "")
            .Replace("@", "")
            .Replace("!", "")
            .Replace("#", "")
            .Replace("$", "")
            .Replace("%", "")
            .Replace("^", "")
            .Replace("&", "")
            .Replace("*", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("-", "")
            .Replace("_", "")
            .Replace("=", "")
            .Replace("+", "");
        
        // Generate password: Welcome@{EmployeeId}123!
        // This format guarantees:
        // - Uppercase: "W"
        // - Lowercase: "elcome"
        // - Numbers: "123"
        // - Special characters: "@" and "!"
        // - Minimum 12 characters
        var password = $"Welcome@{sanitizedEmployeeId}123!";
        
        // Ensure minimum 12 characters - if still short, pad with numbers
        if (password.Length < 12)
        {
            var padding = new string('1', 12 - password.Length);
            password = $"Welcome@{sanitizedEmployeeId}{padding}!";
        }
        
        // Validate password meets all requirements
        if (password.Length < 12)
        {
            throw new InvalidOperationException($"Generated password does not meet minimum length requirement (12 chars). Password length: {password.Length}");
        }
        
        // Truncate if too long (some systems have max length, typically 128 chars)
        if (password.Length > 128)
        {
            password = password.Substring(0, 128);
        }
        
        return password;
    }

    public async Task<EmployeeDto?> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null) return null;
        
        // Validate tenant ownership
        if (employee.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Employee belongs to different tenant");
        }

        _mapper.Map(updateEmployeeDto, employee);
        employee.UpdatedAt = DateTime.UtcNow;
        await _employeeRepository.UpdateAsync(employee);
        return _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<bool> DeleteEmployeeAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null) return false;
        
        // Validate tenant ownership
        if (employee.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Employee belongs to different tenant");
        }

        employee.IsDeleted = true;
        employee.UpdatedAt = DateTime.UtcNow;
        await _employeeRepository.UpdateAsync(employee);
        return true;
    }

    public async Task<bool> EmployeeExistsAsync(int id)
    {
        return await _employeeRepository.ExistsAsync(e => e.Id == id);
    }

    public async Task<bool> EmployeeIdExistsAsync(string employeeId)
    {
        return await _employeeRepository.EmployeeIdExistsAsync(employeeId);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _employeeRepository.EmailExistsAsync(email);
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesByDepartmentAsync(string department)
    {
        var employees = await _employeeRepository.GetByDepartmentAsync(department);
        return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
    }

    public async Task<IEnumerable<EmployeeDto>> GetActiveEmployeesAsync()
    {
        var employees = await _employeeRepository.GetActiveEmployeesAsync();
        return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
    }

    public async Task<IEnumerable<EmployeeDto>> GetInactiveEmployeesAsync()
    {
        var employees = await _employeeRepository.GetInactiveEmployeesAsync();
        return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
    }

    public async Task<PagedResponse<EmployeeDto>> SearchEmployeesAsync(EmployeeSearchRequest request)
    {
        var (employees, totalCount) = await _employeeRepository.SearchEmployeesAsync(
            searchTerm: request.SearchTerm,
            department: request.Department,
            position: request.Position,
            isActive: request.IsActive,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            sortBy: request.SortBy,
            sortDirection: request.SortDirection,
            tenantId: request.TenantId);

        var employeeDtos = _mapper.Map<IEnumerable<EmployeeDto>>(employees);

        return new PagedResponse<EmployeeDto>
        {
            Data = employeeDtos,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    private async Task CheckSubscriptionLimitAsync()
    {
        // Get current tenant's subscription details
        // Try to find tenant by Name matching TenantId, or by Domain
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name.ToLower() == _tenantProvider.TenantId.ToLower() || 
                                      (!string.IsNullOrEmpty(t.Domain) && t.Domain.ToLower() == _tenantProvider.TenantId.ToLower()));

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for TenantId: {TenantId}, using default limits", _tenantProvider.TenantId);
            // If tenant not found, use default limits as fallback
            // This handles the case where there's no tenant setup yet
            return;
        }

        // Check if subscription is active
        if (!tenant.IsSubscriptionActive)
        {
            throw new InvalidOperationException("Your subscription is not active. Please contact support to renew your subscription.");
        }

        // Get current employee count for this tenant
        var currentEmployeeCount = await _employeeRepository.CountAsync(e => e.TenantId == _tenantProvider.TenantId);

        // Check if limit is reached
        if (currentEmployeeCount >= tenant.MaxEmployees)
        {
            throw new InvalidOperationException(
                $"You have reached the maximum number of employees ({tenant.MaxEmployees}) allowed for your {tenant.SubscriptionPlan} subscription plan. " +
                "Please upgrade your subscription to add more employees.");
        }

        _logger.LogInformation("Subscription check passed: {CurrentEmployees}/{MaxEmployees} employees", currentEmployeeCount, tenant.MaxEmployees);
    }
}
