namespace SmallHR.Core.Entities;

public class Module : BaseEntity
{
    public required string TenantId { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public string? ParentPath { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}


