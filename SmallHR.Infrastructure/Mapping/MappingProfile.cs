using AutoMapper;
using SmallHR.Core.DTOs.Attendance;
using SmallHR.Core.DTOs.Auth;
using SmallHR.Core.DTOs.Department;
using SmallHR.Core.DTOs.Employee;
using SmallHR.Core.DTOs.LeaveRequest;
using SmallHR.Core.DTOs.Position;
using SmallHR.Core.Entities;

namespace SmallHR.Infrastructure.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Employee mappings
        CreateMap<Employee, EmployeeDto>();
        CreateMap<CreateEmployeeDto, Employee>();
        CreateMap<UpdateEmployeeDto, Employee>();

        // LeaveRequest mappings
        CreateMap<LeaveRequest, LeaveRequestDto>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => $"{src.Employee.FirstName} {src.Employee.LastName}"));
        CreateMap<CreateLeaveRequestDto, LeaveRequest>();
        CreateMap<UpdateLeaveRequestDto, LeaveRequest>();

        // Attendance mappings
        CreateMap<Attendance, AttendanceDto>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => $"{src.Employee.FirstName} {src.Employee.LastName}"));
        CreateMap<CreateAttendanceDto, Attendance>();
        CreateMap<UpdateAttendanceDto, Attendance>();

        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<RegisterDto, User>();

        // Department mappings
        CreateMap<Department, DepartmentDto>();
        CreateMap<CreateDepartmentDto, Department>();
        CreateMap<UpdateDepartmentDto, Department>();

        // Position mappings
        CreateMap<Position, PositionDto>();
        CreateMap<CreatePositionDto, Position>();
        CreateMap<UpdatePositionDto, Position>();
    }
}
