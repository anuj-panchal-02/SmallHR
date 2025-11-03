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
    private readonly IUserCreationService _userCreationService;
    private readonly ILogger<EmployeeService> _logger;
    private readonly ApplicationDbContext _context;

    public EmployeeService(
        IEmployeeRepository employeeRepository, 
        IMapper mapper, 
        ITenantProvider tenantProvider,
        IUserCreationService userCreationService,
        ILogger<EmployeeService> logger,
        ApplicationDbContext context)
    {
        _employeeRepository = employeeRepository;
        _mapper = mapper;
        _tenantProvider = tenantProvider;
        _userCreationService = userCreationService;
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
        
        // Create or link user automatically for the employee
        User? user = null;
        try
        {
            // Check if user already exists with this email
            user = await _userCreationService.LinkExistingUserAsync(createEmployeeDto.Email, createEmployeeDto.EmployeeId);
            
            if (user == null)
            {
                // Create new user with role assignment
                user = await _userCreationService.CreateUserForEmployeeAsync(createEmployeeDto);
            }
            
            // Link user to employee by setting UserId foreign key
            if (user != null)
            {
                employee.UserId = user.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or linking user for employee {EmployeeId}: {Message}", 
                createEmployeeDto.EmployeeId, ex.Message);
            
            // Re-throw with more context to help debug - this prevents employee creation
            throw new InvalidOperationException($"Failed to create user for employee {createEmployeeDto.EmployeeId}: {ex.Message}", ex);
        }

        // If user was not created/linked, continue without linking but log a warning
        if (user == null || string.IsNullOrEmpty(employee.UserId))
        {
            _logger.LogWarning("Proceeding to create employee {EmployeeId} without linked user account.", createEmployeeDto.EmployeeId);
        }

        await _employeeRepository.AddAsync(employee);
        _logger.LogInformation("Employee {EmployeeId} created successfully with UserId: {UserId}", 
            createEmployeeDto.EmployeeId, employee.UserId);
        
        return _mapper.Map<EmployeeDto>(employee);
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
        // TenantId is stored as string (tenant ID as string), so try to parse and match by ID
        // Also support matching by domain for backward compatibility
        var tenantIdString = _tenantProvider.TenantId;
        
        // Parse tenant ID if possible
        int? tenantIdInt = null;
        if (int.TryParse(tenantIdString, out var parsedId))
        {
            tenantIdInt = parsedId;
        }
        
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => 
                (tenantIdInt.HasValue && t.Id == tenantIdInt.Value) ||
                (!string.IsNullOrEmpty(t.Domain) && t.Domain.ToLower() == tenantIdString.ToLower()));

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
