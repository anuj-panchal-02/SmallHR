using System.Security.Claims;

namespace SmallHR.API.Extensions;

/// <summary>
/// Extension methods for SuperAdmin checks
/// </summary>
public static class SuperAdminExtensions
{
    /// <summary>
    /// Checks if the current user is a SuperAdmin
    /// </summary>
    public static bool IsSuperAdmin(this ClaimsPrincipal user)
    {
        return user?.IsInRole("SuperAdmin") == true;
    }

    /// <summary>
    /// Checks if the current user should bypass tenant isolation
    /// SuperAdmin always bypasses tenant isolation
    /// </summary>
    public static bool ShouldBypassTenantIsolation(this ClaimsPrincipal user)
    {
        return user?.IsInRole("SuperAdmin") == true;
    }

    /// <summary>
    /// Gets the user ID from claims
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value;
    }
}

