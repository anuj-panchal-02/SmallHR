using Microsoft.AspNetCore.Authorization;

namespace SmallHR.API.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeHRAttribute : AuthorizeAttribute
{
    public AuthorizeHRAttribute()
    {
        Roles = "SuperAdmin,Admin,HR";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeAdminAttribute : AuthorizeAttribute
{
    public AuthorizeAdminAttribute()
    {
        Roles = "SuperAdmin,Admin";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeSuperAdminAttribute : AuthorizeAttribute
{
    public AuthorizeSuperAdminAttribute()
    {
        Roles = "SuperAdmin";
    }
}


