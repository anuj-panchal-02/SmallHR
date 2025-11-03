using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Services;

public class TenantFilterService : ITenantFilterService
{
    public string? ResolveTenantIdForRequest(bool isSuperAdmin, string? requestedTenantId)
    {
        if (!isSuperAdmin)
        {
            return null; // normal query filters for non-super admin
        }

        // SuperAdmin: empty string means all tenants; otherwise specific tenant
        return string.IsNullOrWhiteSpace(requestedTenantId) ? string.Empty : requestedTenantId;
    }
}


