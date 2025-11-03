using SmallHR.Core.DTOs.Employee;
using SmallHR.Core.Entities;

namespace SmallHR.Core.Interfaces;

/// <summary>
/// Service for creating and managing user accounts for employees
/// </summary>
public interface IUserCreationService
{
    /// <summary>
    /// Generates a password for a user account based on employee information
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="employeeId">Employee ID</param>
    /// <returns>Generated password string</returns>
    Task<string> GeneratePasswordAsync(string email, string employeeId);

    /// <summary>
    /// Creates a new user account for an employee with role assignment
    /// </summary>
    /// <param name="dto">Employee creation DTO containing user information</param>
    /// <returns>Created User entity, or null if creation failed</returns>
    /// <exception cref="InvalidOperationException">Thrown when user creation fails</exception>
    Task<User?> CreateUserForEmployeeAsync(CreateEmployeeDto dto);

    /// <summary>
    /// Links an existing user account to an employee
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="employeeId">Employee ID for logging</param>
    /// <returns>Existing User entity, or null if not found</returns>
    Task<User?> LinkExistingUserAsync(string email, string employeeId);
}

