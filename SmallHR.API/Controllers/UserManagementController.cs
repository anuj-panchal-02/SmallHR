using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class UserManagementController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserManagementController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult> GetAllUsers()
    {
        try
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

            return Ok(userList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, new { message = "Error retrieving users" });
        }
    }

    /// <summary>
    /// Get all available roles
    /// </summary>
    [HttpGet("roles")]
    public async Task<ActionResult> GetAllRoles()
    {
        try
        {
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            return StatusCode(500, new { message = "Error retrieving roles" });
        }
    }

    /// <summary>
    /// Create new user
    /// </summary>
    [HttpPost("create-user")]
    public async Task<ActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                _logger.LogWarning("Create user validation failed: {Errors}", string.Join(", ", errors));
                return BadRequest(new { 
                    message = "Validation failed", 
                    errors = errors 
                });
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Attempt to create user with existing email: {Email}", request.Email);
                return BadRequest(new { message = "User with this email already exists" });
            }

            // Validate role exists
            var roleExists = await _roleManager.RoleExistsAsync(request.Role);
            if (!roleExists)
            {
                _logger.LogWarning("Attempt to create user with non-existent role: {Role}", request.Role);
                return BadRequest(new { message = $"Role '{request.Role}' does not exist" });
            }

            // Create new user
            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("User creation failed: {Errors}", string.Join(", ", errorMessages));
                return BadRequest(new { 
                    message = "User creation failed", 
                    errors = errorMessages 
                });
            }

            // Assign role
            await _userManager.AddToRoleAsync(user, request.Role);

            _logger.LogInformation("User created successfully: {Email} with role {Role}", request.Email, request.Role);

            return Ok(new { 
                message = "User created successfully", 
                userId = user.Id,
                email = user.Email,
                role = request.Role
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Email}", request.Email);
            return StatusCode(500, new { 
                message = "Internal server error while creating user",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Update user roles
    /// </summary>
    [HttpPut("update-role/{userId}")]
    public async Task<ActionResult> UpdateUserRole(string userId, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Remove all existing roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add new role
            if (!string.IsNullOrEmpty(request.Role))
            {
                var roleExists = await _roleManager.RoleExistsAsync(request.Role);
                if (roleExists)
                {
                    await _userManager.AddToRoleAsync(user, request.Role);
                }
            }

            _logger.LogInformation("User {UserId} role updated to {Role}", userId, request.Role);

            return Ok(new { message = "Role updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role");
            return StatusCode(500, new { message = "Error updating role" });
        }
    }

    /// <summary>
    /// Toggle user active status
    /// </summary>
    [HttpPut("toggle-status/{userId}")]
    public async Task<ActionResult> ToggleUserStatus(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} status toggled to {Status}", userId, user.IsActive);

            return Ok(new { message = "User status updated", isActive = user.IsActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status");
            return StatusCode(500, new { message = "Error updating status" });
        }
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    [HttpPost("reset-password/{userId}")]
    public async Task<ActionResult> ResetPassword(string userId, [FromBody] ResetPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }

            _logger.LogInformation("Password reset for user {UserId}", userId);

            return Ok(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return StatusCode(500, new { message = "Error resetting password" });
        }
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

