using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.DTOs.Department;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantProvider _tenantProvider;

    public DepartmentService(
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository,
        ApplicationDbContext context,
        IMapper mapper,
        ITenantProvider tenantProvider)
    {
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
        _context = context;
        _mapper = mapper;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync()
    {
        var departments = await _departmentRepository.GetAllAsync();
        var departmentDtos = new List<DepartmentDto>();

        foreach (var dept in departments)
        {
            var dto = _mapper.Map<DepartmentDto>(dept);
            dto.HeadOfDepartmentId = dept.HeadOfDepartmentId;
            
            // Get head of department name if assigned
            if (dept.HeadOfDepartmentId.HasValue)
            {
                var headEmployee = await _context.Employees.FindAsync(dept.HeadOfDepartmentId.Value);
                if (headEmployee != null)
                {
                    dto.HeadOfDepartmentName = $"{headEmployee.FirstName} {headEmployee.LastName}";
                }
            }
            
            // Get employee count
            dto.EmployeeCount = await _context.Employees
                .CountAsync(e => e.Department == dept.Name && !e.IsDeleted);
            
            // Get positions for this department
            var positions = await _positionRepository.GetByDepartmentIdAsync(dept.Id);
            dto.Positions = positions.Select(p => p.Title).ToList();
            
            departmentDtos.Add(dto);
        }

        return departmentDtos;
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null) return null;

        // Validate tenant ownership
        if (department.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Department belongs to different tenant");
        }

        var dto = _mapper.Map<DepartmentDto>(department);
        dto.HeadOfDepartmentId = department.HeadOfDepartmentId;
        
        // Get head of department name if assigned
        if (department.HeadOfDepartmentId.HasValue)
        {
            var headEmployee = await _context.Employees.FindAsync(department.HeadOfDepartmentId.Value);
            if (headEmployee != null)
            {
                dto.HeadOfDepartmentName = $"{headEmployee.FirstName} {headEmployee.LastName}";
            }
        }
        
        dto.EmployeeCount = await _context.Employees
            .CountAsync(e => e.Department == department.Name && !e.IsDeleted);
        
        var positions = await _positionRepository.GetByDepartmentIdAsync(department.Id);
        dto.Positions = positions.Select(p => p.Title).ToList();
        
        return dto;
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto createDepartmentDto)
    {
        var department = _mapper.Map<Department>(createDepartmentDto);
        department.TenantId = _tenantProvider.TenantId;
        await _departmentRepository.AddAsync(department);
        return _mapper.Map<DepartmentDto>(department);
    }

    public async Task<DepartmentDto?> UpdateDepartmentAsync(int id, UpdateDepartmentDto updateDepartmentDto)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null) return null;

        // Validate tenant ownership
        if (department.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Department belongs to different tenant");
        }

        _mapper.Map(updateDepartmentDto, department);
        department.UpdatedAt = DateTime.UtcNow;
        await _departmentRepository.UpdateAsync(department);
        return _mapper.Map<DepartmentDto>(department);
    }

    public async Task<bool> DeleteDepartmentAsync(int id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null) return false;

        // Validate tenant ownership
        if (department.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Department belongs to different tenant");
        }

        department.IsDeleted = true;
        department.IsActive = false;
        department.UpdatedAt = DateTime.UtcNow;
        await _departmentRepository.UpdateAsync(department);
        return true;
    }

    public async Task<bool> DepartmentExistsAsync(int id)
    {
        return await _departmentRepository.ExistsAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<IEnumerable<string>> GetDepartmentNamesAsync()
    {
        var departments = await _departmentRepository.GetAllAsync();
        return departments.Where(d => d.IsActive && !d.IsDeleted).Select(d => d.Name);
    }

    public async Task<DepartmentDto?> AssignHeadOfDepartmentAsync(int departmentId, int employeeId)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId);
        if (department == null) return null;

        // Verify employee exists and is in this department
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId && 
                                     e.Department == department.Name && 
                                     !e.IsDeleted && 
                                     e.IsActive);
        
        if (employee == null) return null;

        department.HeadOfDepartmentId = employeeId;
        department.UpdatedAt = DateTime.UtcNow;
        await _departmentRepository.UpdateAsync(department);
        
        return await GetDepartmentByIdAsync(departmentId);
    }

    public async Task<DepartmentDto?> RemoveHeadOfDepartmentAsync(int departmentId)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId);
        if (department == null) return null;

        department.HeadOfDepartmentId = null;
        department.UpdatedAt = DateTime.UtcNow;
        await _departmentRepository.UpdateAsync(department);
        
        return await GetDepartmentByIdAsync(departmentId);
    }
}

