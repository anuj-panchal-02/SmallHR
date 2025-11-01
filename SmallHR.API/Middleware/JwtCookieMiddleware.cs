using Microsoft.AspNetCore.Http;

namespace SmallHR.API.Middleware;

/// <summary>
/// Middleware to extract JWT tokens from httpOnly cookies and add them to the Authorization header.
/// This allows JWT Bearer authentication to work with cookies instead of Authorization headers.
/// </summary>
public class JwtCookieMiddleware
{
    private readonly RequestDelegate _next;

    public JwtCookieMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract access token from cookie and add to Authorization header
        // This allows JWT Bearer authentication to work seamlessly with cookies
        var token = context.Request.Cookies["accessToken"];
        if (!string.IsNullOrEmpty(token) && string.IsNullOrEmpty(context.Request.Headers.Authorization))
        {
            context.Request.Headers["Authorization"] = $"Bearer {token}";
        }

        await _next(context);
    }
}
