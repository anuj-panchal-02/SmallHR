using SmallHR.Core.Entities;

namespace SmallHR.Core.Interfaces;

public interface IPositionRepository : IGenericRepository<Position>
{
    Task<Position?> GetByTitleAsync(string title);
    Task<IEnumerable<Position>> GetByDepartmentIdAsync(int departmentId);
    Task<IEnumerable<Position>> GetActivePositionsAsync();
    Task<bool> TitleExistsAsync(string title);
}

