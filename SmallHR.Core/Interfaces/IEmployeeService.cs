using SmallHR.Core.DTOs;
using SmallHR.Core.DTOs.Employee;

namespace SmallHR.Core.Interfaces;

public interface IEmployeeService : IService
{
    Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync();
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
    Task<EmployeeDto?> GetEmployeeByEmployeeIdAsync(string employeeId);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto);
    Task<EmployeeDto?> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto);
    Task<bool> DeleteEmployeeAsync(int id);
    Task<bool> EmployeeExistsAsync(int id);
    Task<bool> EmployeeIdExistsAsync(string employeeId);
    Task<bool> EmailExistsAsync(string email);
    Task<IEnumerable<EmployeeDto>> GetEmployeesByDepartmentAsync(string department);
    Task<IEnumerable<EmployeeDto>> GetActiveEmployeesAsync();
    Task<IEnumerable<EmployeeDto>> GetInactiveEmployeesAsync();
    
    /// <summary>
    /// Search employees with pagination, filtering, and sorting
    /// </summary>
    Task<PagedResponse<EmployeeDto>> SearchEmployeesAsync(EmployeeSearchRequest request);
}
