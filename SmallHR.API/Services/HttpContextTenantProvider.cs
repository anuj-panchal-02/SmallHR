using Microsoft.AspNetCore.Http;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Services;

public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TenantId
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            var id = ctx?.Items["TenantId"] as string;
            return string.IsNullOrWhiteSpace(id) ? "default" : id!;
        }
    }
}


