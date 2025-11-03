using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Services;

/// <summary>
/// Centralized permission checking service implementation
/// Replaces scattered permission checks across controllers
/// Follows Open/Closed Principle - permission logic is centralized
/// </summary>
public class PermissionService : IPermissionService
{
    private const string SUPER_ADMIN_ROLE = "SuperAdmin";

    public bool IsSuperAdmin(string? userRole)
    {
        return !string.IsNullOrWhiteSpace(userRole) && 
               userRole.Equals(SUPER_ADMIN_ROLE, StringComparison.OrdinalIgnoreCase);
    }

    public bool HasRole(string? userRole, string allowedRoles)
    {
        if (string.IsNullOrWhiteSpace(userRole) || string.IsNullOrWhiteSpace(allowedRoles))
        {
            return false;
        }

        // SuperAdmin always has access
        if (IsSuperAdmin(userRole))
        {
            return true;
        }

        // Check if user's role is in the allowed roles list
        var roles = allowedRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return roles.Any(role => userRole.Equals(role.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public string? GetUserRole(string? userRole)
    {
        return string.IsNullOrWhiteSpace(userRole) ? null : userRole.Trim();
    }
}
