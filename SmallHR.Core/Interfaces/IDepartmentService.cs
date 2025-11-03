using SmallHR.Core.DTOs.Department;

namespace SmallHR.Core.Interfaces;

public interface IDepartmentService : IService
{
    Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync(string? tenantId = null);
    Task<DepartmentDto?> GetDepartmentByIdAsync(int id);
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto createDepartmentDto);
    Task<DepartmentDto?> UpdateDepartmentAsync(int id, UpdateDepartmentDto updateDepartmentDto);
    Task<bool> DeleteDepartmentAsync(int id);
    Task<bool> DepartmentExistsAsync(int id);
    Task<IEnumerable<string>> GetDepartmentNamesAsync();
    Task<DepartmentDto?> AssignHeadOfDepartmentAsync(int departmentId, int employeeId);
    Task<DepartmentDto?> RemoveHeadOfDepartmentAsync(int departmentId);
}

