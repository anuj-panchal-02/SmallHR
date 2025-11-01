namespace SmallHR.Core.Entities;

public class Tenant : BaseEntity
{
    public required string Name { get; set; }
    public string? Domain { get; set; }
    public bool IsActive { get; set; } = true;
}


