using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Repositories;

public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
{
    private readonly ISortStrategyFactory<Employee> _sortStrategyFactory;

    public EmployeeRepository(
        ApplicationDbContext context,
        ISortStrategyFactory<Employee> sortStrategyFactory) : base(context)
    {
        _sortStrategyFactory = sortStrategyFactory;
    }

    public async Task<Employee?> GetByEmployeeIdAsync(string employeeId)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
    }

    public async Task<Employee?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Email == email);
    }

    public async Task<IEnumerable<Employee>> GetByDepartmentAsync(string department)
    {
        return await _dbSet.Where(e => e.Department == department).ToListAsync();
    }

    public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync()
    {
        return await _dbSet.Where(e => e.IsActive).ToListAsync();
    }

    public async Task<IEnumerable<Employee>> GetInactiveEmployeesAsync()
    {
        return await _dbSet.Where(e => !e.IsActive).ToListAsync();
    }

    public async Task<bool> EmployeeIdExistsAsync(string employeeId)
    {
        return await _dbSet.AnyAsync(e => e.EmployeeId == employeeId);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(e => e.Email == email);
    }

    public async Task<(IEnumerable<Employee> Employees, int TotalCount)> SearchEmployeesAsync(
        string? searchTerm = null,
        string? department = null,
        string? position = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = "FirstName",
        string? sortDirection = "asc",
        string? tenantId = null)
    {
        var query = _dbSet.AsQueryable();

        // SuperAdmin filtering logic:
        // - If tenantId is provided (non-empty), ignore query filters and filter by that specific tenant
        // - If tenantId is empty string (""), it means SuperAdmin wants to see ALL tenants - ignore query filters
        // - If tenantId is null, use normal query filters (regular user or tenantId not provided)
        
        // Note: The controller/service will pass empty string ("") when SuperAdmin wants all tenants
        // and a specific tenant name when SuperAdmin wants to filter by tenant
        
        if (tenantId != null)
        {
            // tenantId parameter was provided (SuperAdmin context)
            query = query.IgnoreQueryFilters();
            
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                // Filter to specific tenant
                query = query.Where(e => e.TenantId == tenantId);
            }
            // else tenantId is empty string - show all tenants (no additional filter)
        }
        // else: tenantId is null - use normal query filters (regular user)

        // Filter out deleted employees
        query = query.Where(e => !e.IsDeleted);

        // Apply search term filter (searches across name, email, and employee ID)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchPattern = $"%{searchTerm.Trim()}%";
            
            // Use EF.Functions.Like for SQL Server optimization - search individual fields
            // Searching individual fields to ensure proper SQL translation
            query = query.Where(e =>
                EF.Functions.Like(e.FirstName, searchPattern) ||
                EF.Functions.Like(e.LastName, searchPattern) ||
                EF.Functions.Like(e.Email, searchPattern) ||
                EF.Functions.Like(e.EmployeeId, searchPattern));
        }

        // Apply department filter
        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(e => e.Department == department);
        }

        // Apply position filter
        if (!string.IsNullOrWhiteSpace(position))
        {
            query = query.Where(e => e.Position == position);
        }

        // Apply active status filter
        if (isActive.HasValue)
        {
            query = query.Where(e => e.IsActive == isActive.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplySorting(query, sortBy, sortDirection);

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        var employees = await query.Skip(skip).Take(pageSize).ToListAsync();

        return (employees, totalCount);
    }

    private IQueryable<Employee> ApplySorting(IQueryable<Employee> query, string? sortBy, string? sortDirection)
    {
        sortDirection = string.IsNullOrWhiteSpace(sortDirection) || sortDirection.ToLower() == "asc"
            ? "asc"
            : "desc";

        // Use strategy pattern - follows Open/Closed Principle
        // New sort strategies can be added without modifying this method
        var strategy = _sortStrategyFactory.GetStrategy(sortBy ?? "firstname");
        
        if (strategy == null)
        {
            // Fallback to default strategy if no strategy found
            strategy = _sortStrategyFactory.GetDefaultStrategy();
        }

        return strategy.ApplySort(query, sortDirection);
    }
}
