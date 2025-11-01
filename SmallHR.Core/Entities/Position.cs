using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.Entities;

public class Position : BaseEntity
{
    public required string TenantId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;
    
    public int? DepartmentId { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Department? Department { get; set; }
}

