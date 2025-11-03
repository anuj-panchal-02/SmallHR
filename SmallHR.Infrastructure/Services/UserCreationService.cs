using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SmallHR.Core.DTOs.Employee;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Services;

public class UserCreationService : IUserCreationService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserCreationService> _logger;

    public UserCreationService(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserCreationService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string> GeneratePasswordAsync(string email, string employeeId)
    {
        // Simple password format: Welcome@{EmployeeId}123!
        // This format guarantees:
        // - Uppercase: "W" (from "Welcome")
        // - Lowercase: "elcome" (from "Welcome")
        // - Numbers: "123"
        // - Special characters: "@" and "!"
        // - Minimum 12 characters
        
        // Sanitize employeeId to ensure password validity (remove spaces, special chars that might break password)
        var sanitizedEmployeeId = employeeId
            .Replace(" ", "")
            .Replace("@", "")
            .Replace("!", "")
            .Replace("#", "")
            .Replace("$", "")
            .Replace("%", "")
            .Replace("^", "")
            .Replace("&", "")
            .Replace("*", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("-", "")
            .Replace("_", "")
            .Replace("=", "")
            .Replace("+", "");
        
        // Generate password: Welcome@{EmployeeId}123!
        var password = $"Welcome@{sanitizedEmployeeId}123!";
        
        // Ensure minimum 12 characters - if still short, pad with numbers
        if (password.Length < 12)
        {
            var padding = new string('1', 12 - password.Length);
            password = $"Welcome@{sanitizedEmployeeId}{padding}!";
        }
        
        // Validate password meets all requirements
        if (password.Length < 12)
        {
            throw new InvalidOperationException($"Generated password does not meet minimum length requirement (12 chars). Password length: {password.Length}");
        }
        
        // Truncate if too long (some systems have max length, typically 128 chars)
        if (password.Length > 128)
        {
            password = password.Substring(0, 128);
        }
        
        return Task.FromResult(password);
    }

    public async Task<User?> CreateUserForEmployeeAsync(CreateEmployeeDto dto)
    {
        // Validate role exists
        var roleExists = await _roleManager.RoleExistsAsync(dto.Role);
        if (!roleExists)
        {
            _logger.LogWarning("Role {Role} does not exist for employee {EmployeeId}, defaulting to Employee", dto.Role, dto.EmployeeId);
            dto.Role = "Employee"; // Default to Employee role if invalid
        }

        // Generate a simple password
        var password = await GeneratePasswordAsync(dto.Email, dto.EmployeeId);
        _logger.LogInformation("Generated password for user {Email}: Length={Length}, Format=Welcome@{EmployeeId}123!", 
            dto.Email, password.Length, dto.EmployeeId);
        _logger.LogWarning("USER PASSWORD INFO - Email: {Email}, EmployeeId: {EmployeeId}, Password: {Password}", 
            dto.Email, dto.EmployeeId, password);

        // Create new user
        var user = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Attempting to create user in AspNetUsers: Email={Email}, UserName={UserName}, FirstName={FirstName}, LastName={LastName}, DateOfBirth={DateOfBirth}", 
            user.Email, user.UserName, user.FirstName, user.LastName, user.DateOfBirth);

        var userResult = await _userManager.CreateAsync(user, password);
        
        _logger.LogInformation("UserManager.CreateAsync completed. Succeeded={Succeeded}, Errors={ErrorCount}", 
            userResult.Succeeded, userResult.Errors?.Count() ?? 0);
        
        if (userResult.Succeeded)
        {
            // UserManager.CreateAsync automatically saves to AspNetUsers table
            // The user.Id is now populated with the database-generated ID
            _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);
            
            // Verify user was actually saved by fetching it
            var createdUser = await _userManager.FindByEmailAsync(dto.Email);
            if (createdUser == null)
            {
                _logger.LogError("CRITICAL: User creation succeeded but user not found in database for email {Email}. UserId was: {UserId}", 
                    dto.Email, user.Id);
                throw new InvalidOperationException($"User creation succeeded but user not found in database for {dto.Email}");
            }
            
            _logger.LogInformation("Verified user exists in AspNetUsers table: Email={Email}, Id={UserId}", 
                createdUser.Email, createdUser.Id);
            
            // Assign role to user (this adds entry to AspNetUserRoles table)
            var roleResult = await _userManager.AddToRoleAsync(createdUser, dto.Role);
            if (!roleResult.Succeeded)
            {
                var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign role {Role} to user {Email}: {Errors}", 
                    dto.Role, dto.Email, roleErrors);
                throw new InvalidOperationException($"Failed to assign role {dto.Role} to user {dto.Email}: {roleErrors}");
            }
            
            _logger.LogInformation("Role {Role} assigned to user {Email} (ID: {UserId}). User is in AspNetUsers table.", 
                dto.Role, dto.Email, createdUser.Id);
            
            _logger.LogInformation("SUCCESS: User {Email} (ID: {UserId}) created in AspNetUsers table with role {Role} for employee {EmployeeId}", 
                dto.Email, createdUser.Id, dto.Role, dto.EmployeeId);
            _logger.LogWarning("LOGIN CREDENTIALS - Email: {Email}, Password: Welcome@{EmployeeId}123!, Please use this password to login", 
                dto.Email, dto.EmployeeId);
            
            return createdUser;
        }
        else
        {
            var errors = string.Join(", ", userResult.Errors?.Select(e => $"{e.Code}: {e.Description}") ?? Array.Empty<string>());
            _logger.LogError("Failed to create user for employee {EmployeeId}: {Errors}. User object: Email={Email}, UserName={UserName}, FirstName={FirstName}, LastName={LastName}", 
                dto.EmployeeId, errors, dto.Email, user.UserName, user.FirstName, user.LastName);
            
            // Log password validation details if available
            foreach (var error in userResult.Errors)
            {
                _logger.LogError("Identity error: Code={Code}, Description={Description}", error.Code, error.Description);
            }
            
            // Throw exception to prevent employee creation if user creation fails
            // This ensures data consistency - employee should not exist without user
            throw new InvalidOperationException($"Failed to create user for employee {dto.EmployeeId}: {errors}");
        }
    }

    public async Task<User?> LinkExistingUserAsync(string email, string employeeId)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            _logger.LogInformation("Linked existing user {Email} (ID: {UserId}) to employee {EmployeeId}", 
                email, existingUser.Id, employeeId);
        }
        else
        {
            _logger.LogWarning("User with email {Email} not found when attempting to link to employee {EmployeeId}", 
                email, employeeId);
        }
        
        return existingUser;
    }
}

