using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Repositories;

public class PositionRepository : GenericRepository<Position>, IPositionRepository
{
    public PositionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Position?> GetByTitleAsync(string title)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Title == title);
    }

    public async Task<IEnumerable<Position>> GetByDepartmentIdAsync(int departmentId)
    {
        return await _dbSet
            .Include(p => p.Department)
            .Where(p => p.DepartmentId == departmentId && p.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Position>> GetActivePositionsAsync()
    {
        return await _dbSet.Where(p => p.IsActive).ToListAsync();
    }

    public async Task<bool> TitleExistsAsync(string title)
    {
        return await _dbSet.AnyAsync(p => p.Title == title);
    }

    public new async Task<IEnumerable<Position>> GetAllAsync(string? tenantId = null)
    {
        var query = _dbSet
            .Include(p => p.Department)
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
                query = query.Where(p => p.TenantId == tenantId);
            }
            // else tenantId is empty string - show all tenants (no additional filter)
        }
        // else: tenantId is null - use normal query filters (regular user)

        // Filter out deleted positions
        query = query.Where(p => !p.IsDeleted);

        return await query.ToListAsync();
    }
}

