using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.DTOs.RolePermission;
using SmallHR.Core.Entities;
using SmallHR.Infrastructure.Data;
using SmallHR.API.Authorization;

namespace SmallHR.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RolePermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RolePermissionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/rolepermissions
    [HttpGet]
    [HasPermission("/role-permissions", PermissionAction.View)]
    public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetAllPermissions()
    {
        var permissions = await _context.RolePermissions
            .OrderBy(p => p.RoleName)
            .ThenBy(p => p.PageName)
            .ToListAsync();

        var permissionDtos = permissions.Select(p => new RolePermissionDto
        {
            Id = p.Id,
            RoleName = p.RoleName,
            PagePath = p.PagePath,
            PageName = p.PageName,
            CanAccess = p.CanAccess,
            CanView = p.CanView,
            CanCreate = p.CanCreate,
            CanEdit = p.CanEdit,
            CanDelete = p.CanDelete,
            Description = p.Description
        }).ToList();

        return Ok(permissionDtos);
    }

    // GET: api/rolepermissions/my-permissions
    [HttpGet("my-permissions")]
    public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetMyPermissions()
    {
        // Get the user's role from the JWT token claims
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userRole))
        {
            return BadRequest(new { message = "User role not found in token" });
        }

        var permissions = await _context.RolePermissions
            .Where(p => p.RoleName == userRole)
            .OrderBy(p => p.PageName)
            .ToListAsync();

        var permissionDtos = permissions.Select(p => new RolePermissionDto
        {
            Id = p.Id,
            RoleName = p.RoleName,
            PagePath = p.PagePath,
            PageName = p.PageName,
            CanAccess = p.CanAccess,
            CanView = p.CanView,
            CanCreate = p.CanCreate,
            CanEdit = p.CanEdit,
            CanDelete = p.CanDelete,
            Description = p.Description
        }).ToList();

        return Ok(permissionDtos);
    }

    // GET: api/rolepermissions/role/{roleName}
    // Only SuperAdmin can query permissions for any role
    [HttpGet("role/{roleName}")]
    [HasPermission("/role-permissions", PermissionAction.View)]
    public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetPermissionsByRole(string roleName)
    {
        var permissions = await _context.RolePermissions
            .Where(p => p.RoleName == roleName)
            .OrderBy(p => p.PageName)
            .ToListAsync();

        var permissionDtos = permissions.Select(p => new RolePermissionDto
        {
            Id = p.Id,
            RoleName = p.RoleName,
            PagePath = p.PagePath,
            PageName = p.PageName,
            CanAccess = p.CanAccess,
            CanView = p.CanView,
            CanCreate = p.CanCreate,
            CanEdit = p.CanEdit,
            CanDelete = p.CanDelete,
            Description = p.Description
        }).ToList();

        return Ok(permissionDtos);
    }

    // POST: api/rolepermissions/initialize
    [HttpPost("initialize")]
    [HasPermission("/role-permissions", PermissionAction.Create)]
    public async Task<ActionResult> InitializePermissions()
    {
        // Check if permissions already exist
        if (await _context.RolePermissions.AnyAsync())
        {
            return BadRequest("Permissions already initialized");
        }

        var roles = new[] { "SuperAdmin", "Admin", "HR", "Employee" };
        var pages = new[]
        {
            new { Path = "/dashboard", Name = "Dashboard", Description = "Main dashboard page" },
            new { Path = "/employees", Name = "Employees", Description = "Employee management" },
            new { Path = "/departments", Name = "Departments", Description = "Department management" },
            new { Path = "/positions", Name = "Positions", Description = "Positions management" },
            new { Path = "/calendar", Name = "Calendar", Description = "Calendar and events" },
            new { Path = "/notice-board", Name = "Notice Board", Description = "Company announcements" },
            new { Path = "/expenses", Name = "Expenses", Description = "Expense tracking" },
            new { Path = "/payroll", Name = "Payroll", Description = "Payroll management" },
            new { Path = "/payroll/reports", Name = "Payroll Reports", Description = "Payroll reports" },
            new { Path = "/payroll/settings", Name = "Payroll Settings", Description = "Payroll settings" },
            new { Path = "/settings", Name = "Settings", Description = "User settings" },
            new { Path = "/role-permissions", Name = "Role Permissions", Description = "Manage role permissions" }
        };

        var permissions = new List<RolePermission>();
        var tenantId = HttpContext.Items["TenantId"] as string ?? "default";

        foreach (var role in roles)
        {
            foreach (var page in pages)
            {
                // Determine page access and action-level permissions
                bool canAccess = false;
                bool canView = false;
                bool canCreate = false;
                bool canEdit = false;
                bool canDelete = false;

                // SuperAdmin: full access everywhere
                if (role == "SuperAdmin")
                {
                    canAccess = true;
                    canView = true;
                    canCreate = true;
                    canEdit = true;
                    canDelete = true;
                }
                // Admin: access to all except role-permissions; full actions on allowed pages
                else if (role == "Admin")
                {
                    if (page.Path != "/role-permissions")
                    {
                        canAccess = true;
                        canView = true;
                        canCreate = true;
                        canEdit = true;
                        canDelete = true;
                    }
                }
                // HR: access to selected pages; typically view/create/edit, delete restricted
                else if (role == "HR")
                {
                    if (page.Path == "/dashboard" || page.Path == "/employees" 
                        || page.Path == "/calendar" || page.Path == "/notice-board" 
                        || page.Path == "/settings" || page.Path == "/payroll"
                        || page.Path == "/payroll/reports" || page.Path == "/payroll/settings")
                    {
                        canAccess = true;
                        canView = true;
                        // HR can create/edit on employees, calendar, notice-board, payroll
                        if (page.Path == "/employees" || page.Path == "/calendar" || page.Path == "/notice-board"
                            || page.Path == "/payroll/reports" || page.Path == "/payroll/settings")
                        {
                            canCreate = true;
                            canEdit = true;
                        }
                        // Deletion typically restricted for HR by default
                        canDelete = false;
                    }
                }
                // Employee: limited pages; view only
                else if (role == "Employee")
                {
                    if (page.Path == "/dashboard" || page.Path == "/calendar" 
                        || page.Path == "/notice-board" || page.Path == "/settings")
                    {
                        canAccess = true;
                        canView = true;
                    }
                }

                permissions.Add(new RolePermission
                {
                    TenantId = tenantId,
                    RoleName = role,
                    PagePath = page.Path,
                    PageName = page.Name,
                    CanAccess = canAccess,
                    CanView = canView,
                    CanCreate = canCreate,
                    CanEdit = canEdit,
                    CanDelete = canDelete,
                    Description = page.Description
                });
            }
        }

        _context.RolePermissions.AddRange(permissions);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Permissions initialized successfully", count = permissions.Count });
    }

    // PUT: api/rolepermissions/bulk-update
    [HttpPut("bulk-update")]
    [HasPermission("/role-permissions", PermissionAction.Edit)]
    public async Task<ActionResult> BulkUpdatePermissions([FromBody] BulkUpdateRolePermissionsDto dto)
    {
        foreach (var permission in dto.Permissions)
        {
            var existingPermission = await _context.RolePermissions
                .FirstOrDefaultAsync(p => p.RoleName == permission.RoleName && p.PagePath == permission.PagePath);

            if (existingPermission != null)
            {
                existingPermission.CanAccess = permission.CanAccess;
                if (permission.CanView.HasValue) existingPermission.CanView = permission.CanView.Value;
                if (permission.CanCreate.HasValue) existingPermission.CanCreate = permission.CanCreate.Value;
                if (permission.CanEdit.HasValue) existingPermission.CanEdit = permission.CanEdit.Value;
                if (permission.CanDelete.HasValue) existingPermission.CanDelete = permission.CanDelete.Value;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Permissions updated successfully" });
    }

    // PUT: api/rolepermissions/{id}
    [HttpPut("{id}")]
    [HasPermission("/role-permissions", PermissionAction.Edit)]
    public async Task<ActionResult> UpdatePermission(int id, [FromBody] UpdateRolePermissionDto dto)
    {
        var permission = await _context.RolePermissions.FindAsync(id);
        
        if (permission == null)
        {
            return NotFound();
        }

        permission.CanAccess = dto.CanAccess;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Permission updated successfully" });
    }

    // DELETE: api/rolepermissions/reset
    [HttpDelete("reset")]
    [HasPermission("/role-permissions", PermissionAction.Delete)]
    public async Task<ActionResult> ResetPermissions()
    {
        var permissions = await _context.RolePermissions.ToListAsync();
        _context.RolePermissions.RemoveRange(permissions);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Permissions reset successfully" });
    }

    // POST: api/rolepermissions/add-missing
    [HttpPost("add-missing")]
    [HasPermission("/role-permissions", PermissionAction.Create)]
    public async Task<ActionResult> AddMissingPermissions()
    {
        var pages = new[]
        {
            new { Path = "/dashboard", Name = "Dashboard", Description = "Main dashboard page" },
            new { Path = "/employees", Name = "Employees", Description = "Employee management" },
            new { Path = "/departments", Name = "Departments", Description = "Department management" },
            new { Path = "/positions", Name = "Positions", Description = "Positions management" },
            new { Path = "/calendar", Name = "Calendar", Description = "Calendar and events" },
            new { Path = "/notice-board", Name = "Notice Board", Description = "Company announcements" },
            new { Path = "/expenses", Name = "Expenses", Description = "Expense tracking" },
            new { Path = "/payroll", Name = "Payroll", Description = "Payroll management" },
            new { Path = "/payroll/reports", Name = "Payroll Reports", Description = "Payroll reports" },
            new { Path = "/payroll/settings", Name = "Payroll Settings", Description = "Payroll settings" },
            new { Path = "/settings", Name = "Settings", Description = "User settings" },
            new { Path = "/role-permissions", Name = "Role Permissions", Description = "Manage role permissions" }
        };

        var roles = new[] { "SuperAdmin", "Admin", "HR", "Employee" };
        var newPermissions = new List<RolePermission>();
        var tenantId = HttpContext.Items["TenantId"] as string ?? "default";

        foreach (var role in roles)
        {
            foreach (var page in pages)
            {
                // Check if permission already exists
                var exists = await _context.RolePermissions
                    .AnyAsync(p => p.RoleName == role && p.PagePath == page.Path && p.TenantId == tenantId);

                if (exists) continue;

                // Determine permissions based on role
                bool canAccess = false;
                bool canView = false;
                bool canCreate = false;
                bool canEdit = false;
                bool canDelete = false;

                if (role == "SuperAdmin")
                {
                    canAccess = true; canView = true; canCreate = true; canEdit = true; canDelete = true;
                }
                else if (role == "Admin")
                {
                    if (page.Path != "/role-permissions")
                    {
                        canAccess = true; canView = true; canCreate = true; canEdit = true; canDelete = true;
                    }
                }
                else if (role == "HR")
                {
                    if (page.Path == "/dashboard" || page.Path == "/employees" || page.Path == "/calendar" 
                        || page.Path == "/notice-board" || page.Path == "/settings" || page.Path == "/payroll"
                        || page.Path == "/payroll/reports" || page.Path == "/payroll/settings")
                    {
                        canAccess = true; canView = true;
                        if (page.Path == "/employees" || page.Path == "/calendar" || page.Path == "/notice-board"
                            || page.Path == "/payroll/reports" || page.Path == "/payroll/settings")
                        {
                            canCreate = true; canEdit = true;
                        }
                    }
                }
                else if (role == "Employee")
                {
                    if (page.Path == "/dashboard" || page.Path == "/calendar" 
                        || page.Path == "/notice-board" || page.Path == "/settings")
                    {
                        canAccess = true; canView = true;
                    }
                }

                newPermissions.Add(new RolePermission
                {
                    TenantId = tenantId,
                    RoleName = role,
                    PagePath = page.Path,
                    PageName = page.Name,
                    CanAccess = canAccess,
                    CanView = canView,
                    CanCreate = canCreate,
                    CanEdit = canEdit,
                    CanDelete = canDelete,
                    Description = page.Description
                });
            }
        }

        if (newPermissions.Any())
        {
            _context.RolePermissions.AddRange(newPermissions);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Added {newPermissions.Count} missing permissions", count = newPermissions.Count });
        }

        return Ok(new { message = "No missing permissions found", count = 0 });
    }
}

