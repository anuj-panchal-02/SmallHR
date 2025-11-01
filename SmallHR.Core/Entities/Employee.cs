using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.Entities;

public class Employee : BaseEntity
{
    public required string TenantId { get; set; }
    [Required]
    public string EmployeeId { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    
    public DateTime DateOfBirth { get; set; }
    
    public DateTime HireDate { get; set; }
    
    public DateTime? TerminationDate { get; set; }
    
    [Required]
    public string Position { get; set; } = string.Empty;
    
    [Required]
    public string Department { get; set; } = string.Empty;
    
    public decimal Salary { get; set; }
    
    public string Address { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    
    public string State { get; set; } = string.Empty;
    
    public string ZipCode { get; set; } = string.Empty;
    
    public string Country { get; set; } = string.Empty;
    
    public string EmergencyContactName { get; set; } = string.Empty;
    
    public string EmergencyContactPhone { get; set; } = string.Empty;
    
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    public string Role { get; set; } = "Employee"; // Default role
    
    // Foreign Keys
    public string? UserId { get; set; }
    
    // Navigation properties
    public virtual User? User { get; set; }
    
    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
