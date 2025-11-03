using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AuthorizeSuperAdmin]
public class UserManagementController : BaseApiController
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserManagementController(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserManagementController> logger) : base(logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<object>>> GetAllUsers()
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var users = await _userManager.Users.ToListAsync();
                var userList = new List<object>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userList.Add(new
                    {
                        user.Id,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        user.IsActive,
                        user.CreatedAt,
                        Roles = roles
                    });
                }

                return userList.AsEnumerable();
            },
            "getting all users"
        );
    }

    /// <summary>
    /// Get all available roles
    /// </summary>
    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<string>>> GetAllRoles()
    {
        return await HandleCollectionResultAsync<string>(
            async () => await _roleManager.Roles.Select(r => r.Name!).ToListAsync(),
            "getting all roles"
        );
    }

    /// <summary>
    /// Create new user
    /// </summary>
    [HttpPost("create-user")]
    public async Task<ActionResult<object>> CreateUser([FromBody] CreateUserRequest request)
    {
        // Validate model state
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            
            Logger.LogWarning("Create user validation failed: {Errors}", string.Join(", ", errors));
            return BadRequest(new { 
                message = "Validation failed", 
                errors = errors 
            });
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    Logger.LogWarning("Attempt to create user with existing email: {Email}", request.Email);
                    throw new InvalidOperationException("User with this email already exists");
                }

                // Validate role exists
                var roleExists = await _roleManager.RoleExistsAsync(request.Role);
                if (!roleExists)
                {
                    Logger.LogWarning("Attempt to create user with non-existent role: {Role}", request.Role);
                    throw new InvalidOperationException($"Role '{request.Role}' does not exist");
                }

                // Create new user
                // SuperAdmin users must have TenantId = null (platform layer)
                var user = new User
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DateOfBirth = request.DateOfBirth,
                    IsActive = true,
                    TenantId = request.Role == "SuperAdmin" ? null : null // Will be set by tenant context if needed
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errorMessages = result.Errors.Select(e => e.Description).ToList();
                    Logger.LogWarning("User creation failed: {Errors}", string.Join(", ", errorMessages));
                    throw new InvalidOperationException($"User creation failed: {string.Join(", ", errorMessages)}");
                }

                // Assign role
                await _userManager.AddToRoleAsync(user, request.Role);
                
                // Ensure SuperAdmin has TenantId = null after role assignment
                if (request.Role == "SuperAdmin" && user.TenantId != null)
                {
                    user.TenantId = null;
                    await _userManager.UpdateAsync(user);
                    Logger.LogInformation("Set TenantId = null for SuperAdmin user {Email}", user.Email);
                }

                Logger.LogInformation("User created successfully: {Email} with role {Role}", request.Email, request.Role);

                return new { 
                    message = "User created successfully", 
                    userId = user.Id,
                    email = user.Email,
                    role = request.Role
                };
            },
            "creating user"
        );
    }

    /// <summary>
    /// Update user roles
    /// </summary>
    [HttpPut("update-role/{userId}")]
    public async Task<ActionResult<object>> UpdateUserRole(string userId, [FromBody] UpdateRoleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException("User not found");
                }

                // Remove all existing roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                // Add new role
                if (!string.IsNullOrEmpty(request.Role))
                {
                    var roleExists = await _roleManager.RoleExistsAsync(request.Role);
                    if (!roleExists)
                    {
                        throw new InvalidOperationException($"Role '{request.Role}' does not exist");
                    }
                    await _userManager.AddToRoleAsync(user, request.Role);
                }

                Logger.LogInformation("User {UserId} role updated to {Role}", userId, request.Role);

                return new { message = "Role updated successfully" };
            },
            "updating user role"
        );
    }

    /// <summary>
    /// Toggle user active status
    /// </summary>
    [HttpPut("toggle-status/{userId}")]
    public async Task<ActionResult<object>> ToggleUserStatus(string userId)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException("User not found");
                }

                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);

                Logger.LogInformation("User {UserId} status toggled to {Status}", userId, user.IsActive);

                return new { message = "User status updated", isActive = user.IsActive };
            },
            "toggling user status"
        );
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    [HttpPost("reset-password/{userId}")]
    public async Task<ActionResult<object>> ResetPassword(string userId, [FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException("User not found");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Password reset failed: {errors}");
                }

                Logger.LogInformation("Password reset for user {UserId}", userId);

                return new { message = "Password reset successfully" };
            },
            "resetting user password"
        );
    }
}

// Request DTOs
public class CreateUserRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required")]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Date of birth is required")]
    public DateTime DateOfBirth { get; set; }
    
    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;
}

public class UpdateRoleRequest
{
    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "New password is required")]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters")]
    public string NewPassword { get; set; } = string.Empty;
}

