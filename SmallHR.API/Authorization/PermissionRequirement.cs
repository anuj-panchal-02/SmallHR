using Microsoft.AspNetCore.Authorization;

namespace SmallHR.API.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string PagePath { get; }
    public PermissionAction Action { get; }

    public PermissionRequirement(string pagePath, PermissionAction action)
    {
        PagePath = pagePath;
        Action = action;
    }
}


