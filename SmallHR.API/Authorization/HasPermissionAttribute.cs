using Microsoft.AspNetCore.Authorization;

namespace SmallHR.API.Authorization;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string pagePath, PermissionAction action)
    {
        Policy = $"Permission:{pagePath}:{action}";
    }
}


