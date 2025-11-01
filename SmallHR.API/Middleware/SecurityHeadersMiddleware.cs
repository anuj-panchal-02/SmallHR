using Microsoft.AspNetCore.Http;

namespace SmallHR.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent MIME type sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // Prevent clickjacking attacks
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // Enable XSS protection (legacy but still widely supported)
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Control referrer information
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Prevent browsers from guessing MIME types
        context.Response.Headers.Append("X-Download-Options", "noopen");

        // Disable DNS prefetch
        context.Response.Headers.Append("X-DNS-Prefetch-Control", "off");

        // Prevent Adobe products from accessing content
        context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

        // Permissions Policy (formerly Feature Policy) - control browser features
        context.Response.Headers.Append("Permissions-Policy", 
            "accelerometer=(), " +
            "camera=(), " +
            "geolocation=(), " +
            "gyroscope=(), " +
            "magnetometer=(), " +
            "microphone=(), " +
            "payment=(), " +
            "usb=()");

        // Strict Transport Security - only on HTTPS
        if (context.Request.IsHttps && !context.Request.Host.Host.Contains("localhost"))
        {
            context.Response.Headers.Append("Strict-Transport-Security", 
                "max-age=31536000; includeSubDomains; preload");
        }

        // Content Security Policy
        var csp = BuildCspPolicy();
        context.Response.Headers.Append("Content-Security-Policy", csp);

        await _next(context);
    }

    private string BuildCspPolicy()
    {
        // Build CSP based on environment
        if (_environment.IsDevelopment())
        {
            // Development - more permissive for debugging
            return "default-src 'self'; " +
                   "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                   "style-src 'self' 'unsafe-inline'; " +
                   "img-src 'self' data: https:; " +
                   "font-src 'self' data:; " +
                   "connect-src 'self' http://localhost:* https://localhost:*; " +
                   "frame-ancestors 'none'; " +
                   "base-uri 'self'; " +
                   "form-action 'self'; " +
                   "object-src 'none';";
        }
        else
        {
            // Production - strict policy
            return "default-src 'self'; " +
                   "script-src 'self'; " +
                   "style-src 'self'; " +
                   "img-src 'self' data: https:; " +
                   "font-src 'self' data:; " +
                   "connect-src 'self'; " +
                   "frame-ancestors 'none'; " +
                   "base-uri 'self'; " +
                   "form-action 'self'; " +
                   "object-src 'none'; " +
                   "upgrade-insecure-requests;";
        }
    }
}

