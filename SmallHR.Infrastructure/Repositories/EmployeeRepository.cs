using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Repositories;

public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(ApplicationDbContext context) : base(context)
    {
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
        string? sortDirection = "asc")
    {
        var query = _dbSet.AsQueryable();

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
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            sortBy = "FirstName";
        }

        sortDirection = string.IsNullOrWhiteSpace(sortDirection) || sortDirection.ToLower() == "asc"
            ? "asc"
            : "desc";

        // Use strongly-typed expressions for better SQL translation
        return sortBy.ToLower() switch
        {
            "firstname" => sortDirection == "asc" 
                ? query.OrderBy(e => e.FirstName) 
                : query.OrderByDescending(e => e.FirstName),
            "lastname" => sortDirection == "asc" 
                ? query.OrderBy(e => e.LastName) 
                : query.OrderByDescending(e => e.LastName),
            "email" => sortDirection == "asc" 
                ? query.OrderBy(e => e.Email) 
                : query.OrderByDescending(e => e.Email),
            "employeeid" => sortDirection == "asc" 
                ? query.OrderBy(e => e.EmployeeId) 
                : query.OrderByDescending(e => e.EmployeeId),
            "department" => sortDirection == "asc" 
                ? query.OrderBy(e => e.Department) 
                : query.OrderByDescending(e => e.Department),
            "position" => sortDirection == "asc" 
                ? query.OrderBy(e => e.Position) 
                : query.OrderByDescending(e => e.Position),
            "hiredate" => sortDirection == "asc" 
                ? query.OrderBy(e => e.HireDate) 
                : query.OrderByDescending(e => e.HireDate),
            "salary" => sortDirection == "asc" 
                ? query.OrderBy(e => e.Salary) 
                : query.OrderByDescending(e => e.Salary),
            "createdat" => sortDirection == "asc" 
                ? query.OrderBy(e => e.CreatedAt) 
                : query.OrderByDescending(e => e.CreatedAt),
            _ => sortDirection == "asc" 
                ? query.OrderBy(e => e.FirstName) 
                : query.OrderByDescending(e => e.FirstName)
        };
    }
}
