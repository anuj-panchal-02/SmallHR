using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Base;
using SmallHR.Core.DTOs.RolePermission;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using SmallHR.API.Authorization;

namespace SmallHR.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RolePermissionsController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public RolePermissionsController(
        ApplicationDbContext context, 
        ITenantProvider tenantProvider,
        ILogger<RolePermissionsController> logger) : base(logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    // GET: api/rolepermissions
    [HttpGet]
    [HasPermission("/role-permissions", PermissionAction.View)]
    public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetAllPermissions()
    {
        // Check if user is SuperAdmin - use centralized permission service (follows Open/Closed Principle)
        IQueryable<RolePermission> query = _context.RolePermissions;

        // SuperAdmin can see all permissions across all tenants
        if (IsSuperAdmin)
        {
            query = query.IgnoreQueryFilters();
        }
        // Admin and other roles see only their tenant's permissions (via query filter)

        // For SuperAdmin, filter to show only Admin role permissions
        if (IsSuperAdmin)
        {
            query = query.Where(p => p.RoleName == "Admin");
        }

        var permissions = await query
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
        // Get the user's role from the JWT token claims - use centralized permission service (follows Open/Closed Principle)
        var userRole = CurrentUserRole;
        
        if (string.IsNullOrEmpty(userRole))
        {
            return CreateBadRequestResponse("User role not found in token");
        }

        // SuperAdmin always has access to /role-permissions
        List<RolePermission> permissions;

        if (IsSuperAdmin)
        {
            // SuperAdmin: Get ALL unique page paths from ALL tenants (for menu visibility)
            // This ensures SuperAdmin sees everything, including newly created pages/modules
            var allPermissions = await _context.RolePermissions
                .IgnoreQueryFilters()
                .ToListAsync();

            // Group by pagePath and get unique pages (deduplicate)
            // SuperAdmin should see all pages, regardless of which role/tenant created them
            var uniquePages = allPermissions
                .GroupBy(p => p.PagePath)
                .Select(g => g.First())
                .ToList();

            permissions = uniquePages
                .Select(p => new RolePermission
                {
                    Id = p.Id,
                    TenantId = p.TenantId,
                    RoleName = "SuperAdmin", // Use SuperAdmin role for all
                    PagePath = p.PagePath,
                    PageName = p.PageName,
                    CanAccess = true, // SuperAdmin has access to everything
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true,
                    Description = p.Description
                })
                .OrderBy(p => p.PageName)
                .ToList();

            // Ensure /role-permissions is included
            if (!permissions.Any(p => p.PagePath == "/role-permissions"))
            {
                permissions.Add(new RolePermission
                {
                    Id = 0,
                    TenantId = "platform",
                    RoleName = "SuperAdmin",
                    PagePath = "/role-permissions",
                    PageName = "Role Permissions",
                    CanAccess = true,
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true,
                    Description = "Manage role permissions"
                });
            }
        }
        else
        {
            // For Admin and other roles: get their tenant-specific permissions
            IQueryable<RolePermission> query = _context.RolePermissions
                .Where(p => p.RoleName == userRole);
            // Query filter automatically applies TenantId filter for non-SuperAdmin

            permissions = await query
                .OrderBy(p => p.PageName)
                .ToListAsync();

            // If Admin, add permissions for essential pages if not already present
            if (userRole == "Admin")
            {
                var tenantId = _tenantProvider.TenantId;
                
                // Ensure Dashboard permission exists
                if (!permissions.Any(p => p.PagePath == "/dashboard"))
                {
                    permissions.Add(new RolePermission
                    {
                        Id = 0, // Temporary ID
                        TenantId = tenantId,
                        RoleName = "Admin",
                        PagePath = "/dashboard",
                        PageName = "Dashboard",
                        CanAccess = true,
                        CanView = true,
                        CanCreate = true,
                        CanEdit = true,
                        CanDelete = true,
                        Description = "Main dashboard page"
                    });
                }
                
                // Ensure Role Permissions access exists
                if (!permissions.Any(p => p.PagePath == "/role-permissions"))
                {
                    permissions.Add(new RolePermission
                    {
                        Id = 0, // Temporary ID
                        TenantId = tenantId,
                        RoleName = "Admin",
                        PagePath = "/role-permissions",
                        PageName = "Role Permissions",
                        CanAccess = true,
                        CanView = true,
                        CanCreate = true,
                        CanEdit = true,
                        CanDelete = true,
                        Description = "Manage role permissions"
                    });
                }
            }
        }

        var permissionDtos = permissions
            .GroupBy(p => p.PagePath) // Deduplicate by pagePath
            .Select(g => g.First())
            .Select(p => new RolePermissionDto
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
            })
            .OrderBy(p => p.PageName)
            .ToList();

        return Ok(permissionDtos);
    }

    // GET: api/rolepermissions/role/{roleName}
    // Only SuperAdmin can query permissions for any role
    [HttpGet("role/{roleName}")]
    [HasPermission("/role-permissions", PermissionAction.View)]
    public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetPermissionsByRole(string roleName)
    {
        // Check if user is SuperAdmin - use centralized permission service (follows Open/Closed Principle)
        IQueryable<RolePermission> query = _context.RolePermissions
            .Where(p => p.RoleName == roleName);

        // SuperAdmin can see all permissions across all tenants
        if (IsSuperAdmin)
        {
            query = query.IgnoreQueryFilters();
        }
        // Admin and other roles see only their tenant's permissions (via query filter)

        var permissions = await query
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
        // Use centralized permission service (follows Open/Closed Principle)
        var userRole = CurrentUserRole;
        var isSuperAdmin = IsSuperAdmin;
        var isAdmin = PermissionService.HasRole(userRole, "Admin");
        var tenantId = isSuperAdmin ? "platform" : _tenantProvider.TenantId;

        // Check if permissions already exist for this tenant
        IQueryable<RolePermission> checkQuery = _context.RolePermissions
            .Where(rp => rp.TenantId == tenantId);
        
        // SuperAdmin can see all permissions across all tenants when checking
        if (isSuperAdmin)
        {
            checkQuery = checkQuery.IgnoreQueryFilters();
        }
        
        var existingPermissions = await checkQuery.AnyAsync();
            
        // Allow Admin to initialize even if some permissions exist (might be missing some)
        // But prevent duplicate initialization if all roles have been fully initialized
        if (existingPermissions && !isAdmin)
        {
            // Check if all roles have permissions for this tenant
            var allRolesHavePermissions = await checkQuery
                .GroupBy(rp => rp.RoleName)
                .Select(g => new { RoleName = g.Key, Count = g.Count() })
                .ToListAsync();
            
            // If we have permissions for all 4 roles (SuperAdmin, Admin, HR, Employee), don't allow re-initialization
            if (allRolesHavePermissions.Count >= 4)
            {
                return BadRequest("Permissions already initialized for this tenant");
            }
        }
        
        // For Admin, allow initialization even if permissions exist (in case some are missing)
        // For SuperAdmin, prevent if fully initialized
        if (existingPermissions && isSuperAdmin)
        {
            var roleCount = await checkQuery
                .Select(rp => rp.RoleName)
                .Distinct()
                .CountAsync();
            
            if (roleCount >= 4)
            {
                return BadRequest("Permissions already initialized for this tenant");
            }
        }

        var roles = new[] { "SuperAdmin", "Admin", "HR", "Employee" };
        var pages = new[]
        {
            new { Path = "/dashboard", Name = "Dashboard", Description = "Main dashboard page" },
            new { Path = "/employees", Name = "Employees", Description = "Employee management" },
            new { Path = "/organization", Name = "Organization", Description = "Organization structure management" },
            new { Path = "/departments", Name = "Departments", Description = "Department management" },
            new { Path = "/positions", Name = "Positions", Description = "Positions management" },
            new { Path = "/calendar", Name = "Calendar", Description = "Calendar and events" },
            new { Path = "/notice-board", Name = "Notice Board", Description = "Company announcements" },
            new { Path = "/expenses", Name = "Expenses", Description = "Expense tracking" },
            new { Path = "/payroll", Name = "Payroll", Description = "Payroll management" },
            new { Path = "/payroll/reports", Name = "Payroll Reports", Description = "Payroll reports" },
            new { Path = "/payroll/settings", Name = "Payroll Settings", Description = "Payroll settings" },
            new { Path = "/settings", Name = "Settings", Description = "User settings" },
            new { Path = "/tenant-settings", Name = "Tenant Settings", Description = "Manage tenants" },
            new { Path = "/role-permissions", Name = "Role Permissions", Description = "Manage role permissions" }
        };

        var newPermissions = new List<RolePermission>();
        var updatedCount = 0;
        // tenantId already set above

        foreach (var role in roles)
        {
            foreach (var page in pages)
            {
                // Check if permission already exists
                IQueryable<RolePermission> existsQuery = _context.RolePermissions
                    .Where(p => p.RoleName == role && p.PagePath == page.Path && p.TenantId == tenantId);
                
                // SuperAdmin can see all permissions across all tenants when checking
                if (isSuperAdmin)
                {
                    existsQuery = existsQuery.IgnoreQueryFilters();
                }
                
                var existing = await existsQuery.FirstOrDefaultAsync();

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
                // Admin: access to all except tenant-settings; full actions on allowed pages
                // Note: Admin now has access to /role-permissions
                else if (role == "Admin")
                {
                    if (page.Path != "/tenant-settings")
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

                if (existing != null)
                {
                    // Update existing permission to ensure correct values
                    existing.CanAccess = canAccess;
                    existing.CanView = canView;
                    existing.CanCreate = canCreate;
                    existing.CanEdit = canEdit;
                    existing.CanDelete = canDelete;
                    existing.Description = page.Description;
                    updatedCount++;
                }
                else
                {
                    // Create new permission
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
        }

        if (newPermissions.Any())
        {
            _context.RolePermissions.AddRange(newPermissions);
        }
        
        await _context.SaveChangesAsync();

        var totalCount = newPermissions.Count + updatedCount;
        return Ok(new { message = $"Permissions initialized successfully. Created {newPermissions.Count} new, updated {updatedCount} existing.", count = totalCount });
    }

    // PUT: api/rolepermissions/bulk-update
    [HttpPut("bulk-update")]
    [HasPermission("/role-permissions", PermissionAction.Edit)]
    public async Task<ActionResult> BulkUpdatePermissions([FromBody] BulkUpdateRolePermissionsDto dto)
    {
        // Check if user is SuperAdmin
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var isSuperAdmin = IsSuperAdmin;
        var tenantId = isSuperAdmin ? null : _tenantProvider.TenantId;

        foreach (var permission in dto.Permissions)
        {
            IQueryable<RolePermission> query = _context.RolePermissions
                .Where(p => p.RoleName == permission.RoleName && p.PagePath == permission.PagePath);

            // SuperAdmin can update across all tenants, others only their tenant
            if (!isSuperAdmin)
            {
                query = query.Where(p => p.TenantId == tenantId);
            }
            else
            {
                query = query.IgnoreQueryFilters();
            }

            var existingPermission = await query.FirstOrDefaultAsync();

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
        // Check if user is SuperAdmin
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var isSuperAdmin = IsSuperAdmin;
        var tenantId = isSuperAdmin ? null : _tenantProvider.TenantId;

        IQueryable<RolePermission> query = _context.RolePermissions
            .Where(p => p.Id == id);

        // SuperAdmin can update across all tenants, others only their tenant
        if (!isSuperAdmin)
        {
            query = query.Where(p => p.TenantId == tenantId);
        }
        else
        {
            query = query.IgnoreQueryFilters();
        }

        var permission = await query.FirstOrDefaultAsync();
        
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
        // Check if user is SuperAdmin
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var isSuperAdmin = IsSuperAdmin;
        var tenantId = isSuperAdmin ? null : _tenantProvider.TenantId;

        IQueryable<RolePermission> query = _context.RolePermissions;

        // SuperAdmin can reset across all tenants, others only their tenant
        if (!isSuperAdmin)
        {
            query = query.Where(p => p.TenantId == tenantId);
        }
        else
        {
            query = query.IgnoreQueryFilters();
        }

        var permissions = await query.ToListAsync();
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
            new { Path = "/tenant-settings", Name = "Tenant Settings", Description = "Manage tenants" },
            new { Path = "/role-permissions", Name = "Role Permissions", Description = "Manage role permissions" }
        };

        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var isSuperAdmin = IsSuperAdmin;
        var tenantId = isSuperAdmin ? "platform" : _tenantProvider.TenantId;

        var roles = new[] { "SuperAdmin", "Admin", "HR", "Employee" };
        var newPermissions = new List<RolePermission>();

        foreach (var role in roles)
        {
            foreach (var page in pages)
            {
                // Check if permission already exists
                IQueryable<RolePermission> existsQuery = _context.RolePermissions
                    .Where(p => p.RoleName == role && p.PagePath == page.Path && p.TenantId == tenantId);
                
                // SuperAdmin can see all permissions across all tenants when checking
                if (isSuperAdmin)
                {
                    existsQuery = existsQuery.IgnoreQueryFilters();
                }
                
                var exists = await existsQuery.AnyAsync();

                if (exists) continue;

                // Determine permissions based on role
                bool canAccess = false;
                bool canView = false;
                bool canCreate = false;
                bool canEdit = false;
                bool canDelete = false;

                // SuperAdmin: full access everywhere
                if (role == "SuperAdmin")
                {
                    canAccess = true; canView = true; canCreate = true; canEdit = true; canDelete = true;
                }
                // Admin: access to all except tenant-settings; full actions on allowed pages
                // Note: Admin now has access to /role-permissions
                else if (role == "Admin")
                {
                    if (page.Path != "/tenant-settings")
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

