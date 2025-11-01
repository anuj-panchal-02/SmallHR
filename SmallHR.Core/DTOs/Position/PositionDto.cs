namespace SmallHR.Core.DTOs.Position;

public class PositionDto : BaseDto
{
    public string Title { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
}

public class CreatePositionDto
{
    public required string Title { get; set; }
    public int? DepartmentId { get; set; }
    public string? Description { get; set; }
}

public class UpdatePositionDto
{
    public required string Title { get; set; }
    public int? DepartmentId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

