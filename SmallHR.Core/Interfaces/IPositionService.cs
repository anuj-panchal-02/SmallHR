using SmallHR.Core.DTOs.Position;

namespace SmallHR.Core.Interfaces;

public interface IPositionService : IService
{
    Task<IEnumerable<PositionDto>> GetAllPositionsAsync();
    Task<PositionDto?> GetPositionByIdAsync(int id);
    Task<IEnumerable<PositionDto>> GetPositionsByDepartmentIdAsync(int departmentId);
    Task<PositionDto> CreatePositionAsync(CreatePositionDto createPositionDto);
    Task<PositionDto?> UpdatePositionAsync(int id, UpdatePositionDto updatePositionDto);
    Task<bool> DeletePositionAsync(int id);
    Task<bool> PositionExistsAsync(int id);
    Task<IEnumerable<string>> GetPositionTitlesAsync();
}

