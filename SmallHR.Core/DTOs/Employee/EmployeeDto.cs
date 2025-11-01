using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.DTOs.Employee;

public class EmployeeDto : BaseDto
{
    public string EmployeeId { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string PhoneNumber { get; set; } = string.Empty;
    
    public DateTime DateOfBirth { get; set; }
    
    public DateTime HireDate { get; set; }
    
    public DateTime? TerminationDate { get; set; }
    
    public string Position { get; set; } = string.Empty;
    
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
    
    public bool IsActive { get; set; }
    
    public string Role { get; set; } = string.Empty;
    
    public string? UserId { get; set; }
}

public class CreateEmployeeDto
{
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
    
    [Required]
    public DateTime DateOfBirth { get; set; }
    
    [Required]
    public DateTime HireDate { get; set; }
    
    [Required]
    public string Position { get; set; } = string.Empty;
    
    [Required]
    public string Department { get; set; } = string.Empty;
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number")]
    public decimal Salary { get; set; }
    
    public string Address { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    
    public string State { get; set; } = string.Empty;
    
    public string ZipCode { get; set; } = string.Empty;
    
    public string Country { get; set; } = string.Empty;
    
    public string EmergencyContactName { get; set; } = string.Empty;
    
    public string EmergencyContactPhone { get; set; } = string.Empty;
    
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    
    [Required]
    public string Role { get; set; } = "Employee"; // Default role
}

public class UpdateEmployeeDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required]
    public DateTime DateOfBirth { get; set; }
    
    public DateTime? TerminationDate { get; set; }
    
    [Required]
    public string Position { get; set; } = string.Empty;
    
    [Required]
    public string Department { get; set; } = string.Empty;
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number")]
    public decimal Salary { get; set; }
    
    public string Address { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    
    public string State { get; set; } = string.Empty;
    
    public string ZipCode { get; set; } = string.Empty;
    
    public string Country { get; set; } = string.Empty;
    
    public string EmergencyContactName { get; set; } = string.Empty;
    
    public string EmergencyContactPhone { get; set; } = string.Empty;
    
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    
    public bool IsActive { get; set; }
    
    [Required]
    public string Role { get; set; } = "Employee"; // Default role
}
