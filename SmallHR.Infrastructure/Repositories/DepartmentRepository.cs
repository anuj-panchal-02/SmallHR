using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Repositories;

public class DepartmentRepository : GenericRepository<Department>, IDepartmentRepository
{
    public DepartmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Department?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(d => d.Name == name);
    }

    public async Task<IEnumerable<Department>> GetActiveDepartmentsAsync()
    {
        return await _dbSet.Where(d => d.IsActive).ToListAsync();
    }

    public async Task<bool> NameExistsAsync(string name)
    {
        return await _dbSet.AnyAsync(d => d.Name == name);
    }
}

