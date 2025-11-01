namespace SmallHR.Core.DTOs.Department;

public class DepartmentDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? HeadOfDepartmentId { get; set; }
    public string? HeadOfDepartmentName { get; set; } // Computed: Employee full name
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public List<string> Positions { get; set; } = new();
}

public class CreateDepartmentDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    // HeadOfDepartmentId is optional - can be assigned later via UpdateDepartment or AssignHead endpoint
}

public class UpdateDepartmentDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int? HeadOfDepartmentId { get; set; } // Employee ID - can be null to remove head
    public bool IsActive { get; set; }
}

