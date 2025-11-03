using AutoMapper;
using SmallHR.Core.DTOs.Attendance;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMapper _mapper;
    private readonly ITenantProvider _tenantProvider;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        IMapper mapper,
        ITenantProvider tenantProvider)
    {
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
        _mapper = mapper;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<AttendanceDto>> GetAllAttendanceAsync(string? tenantId = null)
    {
        var attendances = await _attendanceRepository.GetAllAsync(tenantId);
        return _mapper.Map<IEnumerable<AttendanceDto>>(attendances);
    }

    public async Task<AttendanceDto?> GetAttendanceByIdAsync(int id)
    {
        var attendance = await _attendanceRepository.GetByIdAsync(id);
        if (attendance == null) return null;
        
        // Validate tenant ownership
        if (attendance.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Attendance belongs to different tenant");
        }
        
        return _mapper.Map<AttendanceDto>(attendance);
    }

    public async Task<IEnumerable<AttendanceDto>> GetAttendanceByEmployeeIdAsync(int employeeId)
    {
        var attendances = await _attendanceRepository.GetByEmployeeIdAsync(employeeId);
        return _mapper.Map<IEnumerable<AttendanceDto>>(attendances);
    }

    public async Task<AttendanceDto> CreateAttendanceAsync(CreateAttendanceDto createAttendanceDto)
    {
        var attendance = _mapper.Map<Attendance>(createAttendanceDto);
        attendance.IsWeekend = attendance.Date.DayOfWeek == DayOfWeek.Saturday || attendance.Date.DayOfWeek == DayOfWeek.Sunday;
        
        if (attendance.ClockInTime.HasValue && attendance.ClockOutTime.HasValue)
        {
            attendance.TotalHours = attendance.ClockOutTime.Value - attendance.ClockInTime.Value;
            attendance.Status = "Present";
            
            // Calculate overtime (assuming 8 hours is standard work day)
            var standardHours = TimeSpan.FromHours(8);
            if (attendance.TotalHours > standardHours)
            {
                attendance.OvertimeHours = attendance.TotalHours - standardHours;
            }
        }
        else if (attendance.ClockInTime.HasValue)
        {
            attendance.Status = "Present";
        }
        else
        {
            attendance.Status = "Absent";
        }
        
        await _attendanceRepository.AddAsync(attendance);
        return _mapper.Map<AttendanceDto>(attendance);
    }

    public async Task<AttendanceDto?> UpdateAttendanceAsync(int id, UpdateAttendanceDto updateAttendanceDto)
    {
        var attendance = await _attendanceRepository.GetByIdAsync(id);
        if (attendance == null) return null;

        // Validate tenant ownership
        if (attendance.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Attendance belongs to different tenant");
        }

        _mapper.Map(updateAttendanceDto, attendance);
        
        if (attendance.ClockInTime.HasValue && attendance.ClockOutTime.HasValue)
        {
            attendance.TotalHours = attendance.ClockOutTime.Value - attendance.ClockInTime.Value;
            attendance.Status = "Present";
            
            // Calculate overtime
            var standardHours = TimeSpan.FromHours(8);
            if (attendance.TotalHours > standardHours)
            {
                attendance.OvertimeHours = attendance.TotalHours - standardHours;
            }
        }
        
        attendance.UpdatedAt = DateTime.UtcNow;
        await _attendanceRepository.UpdateAsync(attendance);
        return _mapper.Map<AttendanceDto>(attendance);
    }

    public async Task<bool> DeleteAttendanceAsync(int id)
    {
        var attendance = await _attendanceRepository.GetByIdAsync(id);
        if (attendance == null) return false;

        // Validate tenant ownership
        if (attendance.TenantId != _tenantProvider.TenantId)
        {
            throw new UnauthorizedAccessException("Access denied: Attendance belongs to different tenant");
        }

        attendance.IsDeleted = true;
        attendance.UpdatedAt = DateTime.UtcNow;
        await _attendanceRepository.UpdateAsync(attendance);
        return true;
    }

    public async Task<bool> AttendanceExistsAsync(int id)
    {
        return await _attendanceRepository.ExistsAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<AttendanceDto>> GetAttendanceByDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        var attendances = await _attendanceRepository.GetByDateRangeAsync(employeeId, startDate, endDate);
        return _mapper.Map<IEnumerable<AttendanceDto>>(attendances);
    }

    public async Task<AttendanceDto?> GetAttendanceByEmployeeAndDateAsync(int employeeId, DateTime date)
    {
        var attendance = await _attendanceRepository.GetByEmployeeAndDateAsync(employeeId, date);
        return attendance == null ? null : _mapper.Map<AttendanceDto>(attendance);
    }

    public async Task<IEnumerable<AttendanceDto>> GetAttendanceByDateAsync(DateTime date)
    {
        var attendances = await _attendanceRepository.GetByDateAsync(date);
        return _mapper.Map<IEnumerable<AttendanceDto>>(attendances);
    }

    public async Task<IEnumerable<AttendanceDto>> GetAttendanceByMonthAsync(int employeeId, int year, int month)
    {
        var attendances = await _attendanceRepository.GetByMonthAsync(employeeId, year, month);
        return _mapper.Map<IEnumerable<AttendanceDto>>(attendances);
    }

    public async Task<AttendanceDto> ClockInAsync(ClockInDto clockInDto)
    {
        var existingAttendance = await _attendanceRepository.GetByEmployeeAndDateAsync(clockInDto.EmployeeId, clockInDto.ClockInTime.Date);
        
        if (existingAttendance != null)
        {
            existingAttendance.ClockInTime = clockInDto.ClockInTime;
            existingAttendance.Status = "Present";
            existingAttendance.UpdatedAt = DateTime.UtcNow;
            await _attendanceRepository.UpdateAsync(existingAttendance);
            return _mapper.Map<AttendanceDto>(existingAttendance);
        }
        else
        {
            var attendance = new Attendance
            {
                TenantId = _tenantProvider.TenantId,
                EmployeeId = clockInDto.EmployeeId,
                Date = clockInDto.ClockInTime.Date,
                ClockInTime = clockInDto.ClockInTime,
                Status = "Present",
                IsWeekend = clockInDto.ClockInTime.DayOfWeek == DayOfWeek.Saturday || clockInDto.ClockInTime.DayOfWeek == DayOfWeek.Sunday
            };
            
            await _attendanceRepository.AddAsync(attendance);
            return _mapper.Map<AttendanceDto>(attendance);
        }
    }

    public async Task<AttendanceDto?> ClockOutAsync(ClockOutDto clockOutDto)
    {
        var attendance = await _attendanceRepository.GetByEmployeeAndDateAsync(clockOutDto.EmployeeId, clockOutDto.ClockOutTime.Date);
        if (attendance == null) return null;

        attendance.ClockOutTime = clockOutDto.ClockOutTime;
        
        if (attendance.ClockInTime.HasValue)
        {
            attendance.TotalHours = attendance.ClockOutTime.Value - attendance.ClockInTime.Value;
            
            // Calculate overtime
            var standardHours = TimeSpan.FromHours(8);
            if (attendance.TotalHours > standardHours)
            {
                attendance.OvertimeHours = attendance.TotalHours - standardHours;
            }
        }
        
        attendance.UpdatedAt = DateTime.UtcNow;
        await _attendanceRepository.UpdateAsync(attendance);
        return _mapper.Map<AttendanceDto>(attendance);
    }

    public async Task<TimeSpan> GetTotalHoursAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        return await _attendanceRepository.GetTotalHoursAsync(employeeId, startDate, endDate);
    }

    public async Task<TimeSpan> GetOvertimeHoursAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        return await _attendanceRepository.GetOvertimeHoursAsync(employeeId, startDate, endDate);
    }

    public async Task<bool> HasClockInAsync(int employeeId, DateTime date)
    {
        return await _attendanceRepository.HasClockInAsync(employeeId, date);
    }

    public async Task<bool> HasClockOutAsync(int employeeId, DateTime date)
    {
        return await _attendanceRepository.HasClockOutAsync(employeeId, date);
    }
}
