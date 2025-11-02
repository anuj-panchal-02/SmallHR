using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmallHR.Infrastructure.Data;

namespace SmallHR.API.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _db;

    public PermissionAuthorizationHandler(ApplicationDbContext db)
    {
        _db = db;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(role))
        {
            return; // no role claim
        }

        // SuperAdmin shortcut
        if (role == "SuperAdmin")
        {
            context.Succeed(requirement);
            return;
        }

        // Special case: Allow Admin to access essential pages even if permissions are missing or incorrectly set
        // This is needed to ensure Admin can always access critical functionality
        
        // Allow Admin to access dashboard and role-permissions even if permissions don't exist or are false
        if (role == "Admin" && (requirement.PagePath == "/role-permissions" || requirement.PagePath == "/dashboard"))
        {
            // Check if Admin has the permission
            var perm = await _db.RolePermissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.RoleName == role && p.PagePath == requirement.PagePath);

            // For role-permissions: Allow Admin to view and initialize even if permission doesn't exist or is false
            if (requirement.PagePath == "/role-permissions")
            {
                if (perm == null)
                {
                    // Permission doesn't exist - allow Admin to proceed (they can initialize)
                    context.Succeed(requirement);
                    return;
                }

                // Permission exists, check if the specific action is allowed
                if (requirement.Action == PermissionAction.Create && !perm.CanCreate)
                {
                    // Allow Admin to initialize even if CanCreate is false
                    context.Succeed(requirement);
                    return;
                }
                
                if (requirement.Action == PermissionAction.View && !perm.CanView && !perm.CanAccess)
                {
                    // Allow Admin to view even if CanView/CanAccess is false (needed for initialization)
                    context.Succeed(requirement);
                    return;
                }
            }
            
            // For dashboard: Always allow Admin to view (critical for login flow)
            if (requirement.PagePath == "/dashboard" && requirement.Action == PermissionAction.View)
            {
                if (perm == null || !perm.CanView && !perm.CanAccess)
                {
                    // Allow Admin to access dashboard even if permission doesn't exist or is false
                    context.Succeed(requirement);
                    return;
                }
            }
        }

        var perm2 = await _db.RolePermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.RoleName == role && p.PagePath == requirement.PagePath);

        if (perm2 == null)
        {
            return;
        }

        var allowed = requirement.Action switch
        {
            PermissionAction.View => perm2.CanView || perm2.CanAccess,
            PermissionAction.Create => perm2.CanCreate,
            PermissionAction.Edit => perm2.CanEdit,
            PermissionAction.Delete => perm2.CanDelete,
            _ => false
        };

        if (allowed)
        {
            context.Succeed(requirement);
        }
    }
}


