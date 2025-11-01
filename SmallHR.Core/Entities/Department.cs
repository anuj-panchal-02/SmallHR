using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.Entities;

public class Department : BaseEntity
{
    public required string TenantId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public int? HeadOfDepartmentId { get; set; } // Employee ID - optional, can be assigned later
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Employee? HeadOfDepartment { get; set; }
    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();
}

