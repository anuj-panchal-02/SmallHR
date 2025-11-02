using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Infrastructure.Data;

namespace SmallHR.API.Controllers;

/// <summary>
/// Admin utilities for SuperAdmin
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<User> userManager,
        ApplicationDbContext context,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Fix SuperAdmin users to have TenantId = NULL
    /// This ensures SuperAdmin users operate at platform layer
    /// </summary>
    [HttpPost("fix-superadmin-tenantid")]
    public async Task<IActionResult> FixSuperAdminTenantId()
    {
        try
        {
            var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
            if (superAdminRole == null)
            {
                return NotFound(new { message = "SuperAdmin role not found" });
            }

            // Get all users with SuperAdmin role
            var superAdminUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == superAdminRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var superAdminUsers = await _userManager.Users
                .Where(u => superAdminUserIds.Contains(u.Id))
                .ToListAsync();

            var updatedCount = 0;
            var fixedUsers = new List<object>();

            foreach (var user in superAdminUsers)
            {
                if (user.TenantId != null)
                {
                    user.TenantId = null;
                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        updatedCount++;
                        fixedUsers.Add(new
                        {
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            PreviousTenantId = user.TenantId ?? "null"
                        });
                        _logger.LogInformation("Fixed SuperAdmin user {Email} - set TenantId to NULL", user.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update SuperAdmin user {Email}: {Errors}",
                            user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            return Ok(new
            {
                message = "SuperAdmin users fixed successfully",
                updatedCount = updatedCount,
                totalSuperAdmins = superAdminUsers.Count,
                fixedUsers = fixedUsers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing SuperAdmin TenantId");
            return StatusCode(500, new { message = "Error fixing SuperAdmin TenantId", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all SuperAdmin users and their TenantId status
    /// </summary>
    [HttpGet("superadmin-users")]
    public async Task<IActionResult> GetSuperAdminUsers()
    {
        try
        {
            var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
            if (superAdminRole == null)
            {
                return NotFound(new { message = "SuperAdmin role not found" });
            }

            var superAdminUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == superAdminRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var superAdminUsers = await _userManager.Users
                .Where(u => superAdminUserIds.Contains(u.Id))
                .ToListAsync();

            var users = new List<object>();
            foreach (var user in superAdminUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                users.Add(new
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    TenantId = user.TenantId,
                    IsCorrect = user.TenantId == null,
                    Roles = roles,
                    IsActive = user.IsActive
                });
            }

            var needsFix = users.Count(u => ((dynamic)u).IsCorrect == false);

            return Ok(new
            {
                totalSuperAdmins = superAdminUsers.Count,
                needsFix = needsFix,
                users = users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SuperAdmin users");
            return StatusCode(500, new { message = "Error getting SuperAdmin users", error = ex.Message });
        }
    }

    /// <summary>
    /// Verify SuperAdmin configuration
    /// </summary>
    [HttpGet("verify-superadmin")]
    public async Task<IActionResult> VerifySuperAdmin()
    {
        try
        {
            var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
            if (superAdminRole == null)
            {
                return Ok(new
                {
                    isValid = false,
                    issues = new[] { "SuperAdmin role not found" }
                });
            }

            var superAdminUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == superAdminRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var superAdminUsers = await _userManager.Users
                .Where(u => superAdminUserIds.Contains(u.Id))
                .ToListAsync();

            var issues = new List<string>();
            var correctUsers = 0;

            foreach (var user in superAdminUsers)
            {
                if (user.TenantId != null)
                {
                    issues.Add($"SuperAdmin user {user.Email} has TenantId = '{user.TenantId}' (should be NULL)");
                }
                else
                {
                    correctUsers++;
                }
            }

            if (superAdminUsers.Count == 0)
            {
                issues.Add("No SuperAdmin users found");
            }

            return Ok(new
            {
                isValid = issues.Count == 0,
                superAdminRoleExists = superAdminRole != null,
                totalSuperAdmins = superAdminUsers.Count,
                correctConfiguration = correctUsers,
                needsFix = superAdminUsers.Count - correctUsers,
                issues = issues
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying SuperAdmin configuration");
            return StatusCode(500, new { message = "Error verifying SuperAdmin configuration", error = ex.Message });
        }
    }
}

