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

        var perm = await _db.RolePermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.RoleName == role && p.PagePath == requirement.PagePath);

        if (perm == null)
        {
            return;
        }

        var allowed = requirement.Action switch
        {
            PermissionAction.View => perm.CanView || perm.CanAccess,
            PermissionAction.Create => perm.CanCreate,
            PermissionAction.Edit => perm.CanEdit,
            PermissionAction.Delete => perm.CanDelete,
            _ => false
        };

        if (allowed)
        {
            context.Succeed(requirement);
        }
    }
}


