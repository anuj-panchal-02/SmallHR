namespace SmallHR.Core.Interfaces;

public interface ITenantFilterService
{
    /// <summary>
    /// Computes the tenantId parameter to pass downstream based on current user's role and requested tenantId.
    /// Returns null for non-SuperAdmin (use default query filters),
    /// empty string "" for SuperAdmin requesting all tenants,
    /// or the provided tenantId when SuperAdmin filters a specific tenant.
    /// </summary>
    /// <param name="isSuperAdmin">Whether current user is SuperAdmin</param>
    /// <param name="requestedTenantId">Query parameter tenantId</param>
    /// <returns>string? tenantIdForRequest per convention</returns>
    string? ResolveTenantIdForRequest(bool isSuperAdmin, string? requestedTenantId);
}


