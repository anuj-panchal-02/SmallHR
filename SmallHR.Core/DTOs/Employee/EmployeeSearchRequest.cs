using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.DTOs.Employee;

/// <summary>
/// Request DTO for searching and filtering employees with pagination
/// </summary>
public class EmployeeSearchRequest
{
    /// <summary>
    /// Search term to search across name, email, and employee ID
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Filter by department
    /// </summary>
    public string? Department { get; set; }
    
    /// <summary>
    /// Filter by position
    /// </summary>
    public string? Position { get; set; }
    
    /// <summary>
    /// Filter by active status (null = all)
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Page number (1-indexed)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Sort field (FirstName, LastName, Email, Department, Position, HireDate)
    /// </summary>
    public string? SortBy { get; set; } = "FirstName";
    
    /// <summary>
    /// Sort direction (asc or desc)
    /// </summary>
    public string? SortDirection { get; set; } = "asc";
    
    /// <summary>
    /// Filter by tenant ID (SuperAdmin only)
    /// </summary>
    public string? TenantId { get; set; }
}

