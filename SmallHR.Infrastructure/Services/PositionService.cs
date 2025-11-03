using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.DTOs.Position;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Services;

public class PositionService : IPositionService
{
    private readonly IPositionRepository _positionRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantProvider _tenantProvider;

    public PositionService(
        IPositionRepository positionRepository,
        IDepartmentRepository departmentRepository,
        ApplicationDbContext context,
        IMapper mapper,
        ITenantProvider tenantProvider)
    {
        _positionRepository = positionRepository;
        _departmentRepository = departmentRepository;
        _context = context;
        _mapper = mapper;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<PositionDto>> GetAllPositionsAsync(string? tenantId = null)
    {
        var positions = await _positionRepository.GetAllAsync(tenantId);
        var positionDtos = new List<PositionDto>();

        foreach (var pos in positions)
        {
            var dto = _mapper.Map<PositionDto>(pos);
            
            if (pos.DepartmentId.HasValue)
            {
                var dept = await _departmentRepository.GetByIdAsync(pos.DepartmentId.Value);
                dto.DepartmentName = dept?.Name;
            }
            
            // Get employee count (respect tenant filter if provided)
            var employeeQuery = _context.Employees.Where(e => e.Position == pos.Title && !e.IsDeleted);
            if (tenantId != null)
            {
                employeeQuery = employeeQuery.IgnoreQueryFilters();
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    employeeQuery = employeeQuery.Where(e => e.TenantId == tenantId);
                }
            }
            dto.EmployeeCount = await employeeQuery.CountAsync();
            
            positionDtos.Add(dto);
        }

        return positionDtos;
    }

    public async Task<PositionDto?> GetPositionByIdAsync(int id)
    {
        var position = await _positionRepository.GetByIdAsync(id);
        if (position == null) return null;

        // Validate tenant ownership
        if (position.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Position belongs to different tenant");
        }

        var dto = _mapper.Map<PositionDto>(position);
        
        if (position.DepartmentId.HasValue)
        {
            var dept = await _departmentRepository.GetByIdAsync(position.DepartmentId.Value);
            dto.DepartmentName = dept?.Name;
        }
        
        dto.EmployeeCount = await _context.Employees
            .CountAsync(e => e.Position == position.Title && !e.IsDeleted);
        
        return dto;
    }

    public async Task<IEnumerable<PositionDto>> GetPositionsByDepartmentIdAsync(int departmentId)
    {
        var positions = await _positionRepository.GetByDepartmentIdAsync(departmentId);
        var department = await _departmentRepository.GetByIdAsync(departmentId);
        
        return positions.Select(p => new PositionDto
        {
            Id = p.Id,
            Title = p.Title,
            DepartmentId = p.DepartmentId,
            DepartmentName = department?.Name,
            Description = p.Description,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            EmployeeCount = _context.Employees.Count(e => e.Position == p.Title && !e.IsDeleted)
        });
    }

    public async Task<PositionDto> CreatePositionAsync(CreatePositionDto createPositionDto)
    {
        var position = _mapper.Map<Position>(createPositionDto);
        position.TenantId = _tenantProvider.TenantId;
        await _positionRepository.AddAsync(position);
        
        var dto = _mapper.Map<PositionDto>(position);
        if (position.DepartmentId.HasValue)
        {
            var dept = await _departmentRepository.GetByIdAsync(position.DepartmentId.Value);
            dto.DepartmentName = dept?.Name;
        }
        
        return dto;
    }

    public async Task<PositionDto?> UpdatePositionAsync(int id, UpdatePositionDto updatePositionDto)
    {
        var position = await _positionRepository.GetByIdAsync(id);
        if (position == null) return null;

        // Validate tenant ownership
        if (position.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Position belongs to different tenant");
        }

        _mapper.Map(updatePositionDto, position);
        position.UpdatedAt = DateTime.UtcNow;
        await _positionRepository.UpdateAsync(position);
        
        var dto = _mapper.Map<PositionDto>(position);
        if (position.DepartmentId.HasValue)
        {
            var dept = await _departmentRepository.GetByIdAsync(position.DepartmentId.Value);
            dto.DepartmentName = dept?.Name;
        }
        
        return dto;
    }

    public async Task<bool> DeletePositionAsync(int id)
    {
        var position = await _positionRepository.GetByIdAsync(id);
        if (position == null) return false;

        // Validate tenant ownership
        if (position.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Position belongs to different tenant");
        }

        position.IsDeleted = true;
        position.IsActive = false;
        position.UpdatedAt = DateTime.UtcNow;
        await _positionRepository.UpdateAsync(position);
        return true;
    }

    public async Task<bool> PositionExistsAsync(int id)
    {
        return await _positionRepository.ExistsAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<IEnumerable<string>> GetPositionTitlesAsync()
    {
        var positions = await _positionRepository.GetAllAsync();
        return positions.Where(p => p.IsActive && !p.IsDeleted).Select(p => p.Title);
    }
}

