namespace SmallHR.Core.Interfaces;

/// <summary>
/// Centralized permission checking service
/// Follows Open/Closed Principle - permission logic is centralized and extensible
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Checks if the current user is SuperAdmin
    /// </summary>
    /// <param name="userRole">The user's role claim value</param>
    /// <returns>True if user is SuperAdmin</returns>
    bool IsSuperAdmin(string? userRole);

    /// <summary>
    /// Checks if the current user has one of the specified roles
    /// </summary>
    /// <param name="userRole">The user's role claim value</param>
    /// <param name="allowedRoles">Comma-separated list of allowed roles</param>
    /// <returns>True if user has one of the allowed roles</returns>
    bool HasRole(string? userRole, string allowedRoles);

    /// <summary>
    /// Gets the current user's role from claims
    /// </summary>
    /// <param name="userRole">The user's role claim value (can be null)</param>
    /// <returns>The user's role, or null if not found</returns>
    string? GetUserRole(string? userRole);
}
