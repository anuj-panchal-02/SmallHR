using SmallHR.Core.DTOs.Employee;
using SmallHR.Core.Entities;

namespace SmallHR.Core.Interfaces;

public interface IEmployeeRepository : IGenericRepository<Employee>
{
    Task<Employee?> GetByEmployeeIdAsync(string employeeId);
    Task<Employee?> GetByEmailAsync(string email);
    Task<IEnumerable<Employee>> GetByDepartmentAsync(string department);
    Task<IEnumerable<Employee>> GetActiveEmployeesAsync();
    Task<IEnumerable<Employee>> GetInactiveEmployeesAsync();
    Task<bool> EmployeeIdExistsAsync(string employeeId);
    Task<bool> EmailExistsAsync(string email);
    
    /// <summary>
    /// Search employees with pagination, filtering, and sorting
    /// </summary>
    Task<(IEnumerable<Employee> Employees, int TotalCount)> SearchEmployeesAsync(
        string? searchTerm = null,
        string? department = null,
        string? position = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = "FirstName",
        string? sortDirection = "asc");
}
