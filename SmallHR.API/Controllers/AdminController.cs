using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.Entities;
using SmallHR.Infrastructure.Data;

namespace SmallHR.API.Controllers;

/// <summary>
/// Admin utilities for SuperAdmin
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AuthorizeSuperAdmin]
public class AdminController : BaseApiController
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _context;

    public AdminController(
        UserManager<User> userManager,
        ApplicationDbContext context,
        ILogger<AdminController> logger) : base(logger)
    {
        _userManager = userManager;
        _context = context;
    }

    /// <summary>
    /// Fix SuperAdmin users to have TenantId = NULL
    /// This ensures SuperAdmin users operate at platform layer
    /// </summary>
    [HttpPost("fix-superadmin-tenantid")]
    public async Task<ActionResult<object>> FixSuperAdminTenantId()
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
                if (superAdminRole == null)
                {
                    throw new KeyNotFoundException("SuperAdmin role not found");
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
                        var previousTenantId = user.TenantId;
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
                                PreviousTenantId = previousTenantId ?? "null"
                            });
                            Logger.LogInformation("Fixed SuperAdmin user {Email} - set TenantId to NULL", user.Email);
                        }
                        else
                        {
                            Logger.LogWarning("Failed to update SuperAdmin user {Email}: {Errors}",
                                user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }

                return new
                {
                    message = "SuperAdmin users fixed successfully",
                    updatedCount = updatedCount,
                    totalSuperAdmins = superAdminUsers.Count,
                    fixedUsers = fixedUsers
                };
            },
            "fixing SuperAdmin TenantId"
        );
    }

    /// <summary>
    /// Get all SuperAdmin users and their TenantId status
    /// </summary>
    [HttpGet("superadmin-users")]
    public async Task<ActionResult<object>> GetSuperAdminUsers()
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
                if (superAdminRole == null)
                {
                    throw new KeyNotFoundException("SuperAdmin role not found");
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

                return new
                {
                    totalSuperAdmins = superAdminUsers.Count,
                    needsFix = needsFix,
                    users = users
                };
            },
            "getting SuperAdmin users"
        );
    }

    /// <summary>
    /// Verify SuperAdmin configuration
    /// </summary>
    [HttpGet("verify-superadmin")]
    public async Task<ActionResult<object>> VerifySuperAdmin()
    {
        return await HandleServiceResultAsync<object>(
            async () =>
            {
                var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
                if (superAdminRole == null)
                {
                    return new
                    {
                        isValid = false,
                        issues = new[] { "SuperAdmin role not found" }
                    };
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

                return new
                {
                    isValid = issues.Count == 0,
                    superAdminRoleExists = superAdminRole != null,
                    totalSuperAdmins = superAdminUsers.Count,
                    correctConfiguration = correctUsers,
                    needsFix = superAdminUsers.Count - correctUsers,
                    issues = issues
                } as object;
            },
            "verifying SuperAdmin configuration"
        );
    }
}

