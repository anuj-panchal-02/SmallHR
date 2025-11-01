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
}

