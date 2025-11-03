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

    public new async Task<IEnumerable<Department>> GetAllAsync(string? tenantId = null)
    {
        var query = _dbSet
            .Include(d => d.HeadOfDepartment)
            .AsQueryable();

        // SuperAdmin filtering logic:
        // - If tenantId is provided (non-empty), ignore query filters and filter by that specific tenant
        // - If tenantId is empty string (""), it means SuperAdmin wants to see ALL tenants - ignore query filters
        // - If tenantId is null, use normal query filters (regular user or tenantId not provided)
        
        if (tenantId != null)
        {
            // tenantId parameter was provided (SuperAdmin context)
            query = query.IgnoreQueryFilters();
            
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                // Filter to specific tenant
                query = query.Where(d => d.TenantId == tenantId);
            }
            // else tenantId is empty string - show all tenants (no additional filter)
        }
        // else: tenantId is null - use normal query filters (regular user)

        // Filter out deleted departments
        query = query.Where(d => !d.IsDeleted);

        return await query.ToListAsync();
    }
}

