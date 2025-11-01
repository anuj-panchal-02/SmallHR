using SmallHR.Core.Entities;

namespace SmallHR.Core.Interfaces;

public interface IDepartmentRepository : IGenericRepository<Department>
{
    Task<Department?> GetByNameAsync(string name);
    Task<IEnumerable<Department>> GetActiveDepartmentsAsync();
    Task<bool> NameExistsAsync(string name);
}

