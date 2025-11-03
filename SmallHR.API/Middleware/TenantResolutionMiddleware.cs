using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SmallHR.Core.Interfaces;
using SmallHR.API.Extensions;

namespace SmallHR.API.Middleware;

/// <summary>
/// Tenant Resolution Middleware
/// 
/// Detects tenant context from multiple sources in priority order:
/// 1. JWT Claims (TenantId or tenant claim) - when authenticated
/// 2. Request subdomain (tenantname.yourapp.com)
/// 3. X-Tenant-Id header
/// 4. X-Tenant-Domain header
/// 5. Default tenant ("default")
/// 
/// Enforces tenant boundary: If authenticated, JWT tenant claim must match resolved tenant.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    
    public TenantResolutionMiddleware(RequestDelegate next) 
    { 
        _next = next; 
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
        var isSuperAdmin = context.User?.IsInRole("SuperAdmin") == true;
        var isImpersonating = context.User?.FindFirst("IsImpersonating")?.Value == "true";

        // Handle impersonation mode
        // SuperAdmin with impersonation token acts as the impersonated tenant
        if (isAuthenticated && isSuperAdmin && isImpersonating)
        {
            var impersonatedTenantId = context.User.FindFirst("TenantId")?.Value 
                ?? context.User.FindFirst("tenant")?.Value;
            
            if (!string.IsNullOrWhiteSpace(impersonatedTenantId) && impersonatedTenantId != "platform")
            {
                context.Items["TenantId"] = impersonatedTenantId;
                context.Items["IsSuperAdmin"] = true;
                context.Items["IsImpersonating"] = true;
                context.Items["OriginalUserId"] = context.User.FindFirst("OriginalUserId")?.Value;
                context.Items["OriginalEmail"] = context.User.FindFirst("OriginalEmail")?.Value;
                await _next(context);
                return;
            }
        }

        // SuperAdmin without impersonation - operates at platform layer
        if (isSuperAdmin && !isImpersonating)
        {
            // SuperAdmin operates at platform layer - no tenant context
            context.Items["TenantId"] = "platform"; // Special marker for SuperAdmin
            context.Items["IsSuperAdmin"] = true;
            await _next(context);
            return;
        }

        string? resolvedTenantId = null;
        string? source = null;

        // Priority 1: JWT Claims (if authenticated) - Most authoritative
        // Check both "TenantId" and "tenant" claims for compatibility
        if (isAuthenticated)
        {
            var jwtTenant = context.User.FindFirst("TenantId")?.Value 
                ?? context.User.FindFirst("tenant")?.Value;
            
            if (!string.IsNullOrWhiteSpace(jwtTenant) && jwtTenant != "platform")
            {
                resolvedTenantId = jwtTenant;
                source = "JWT_CLAIM";
            }
        }

        // Priority 2: Subdomain detection (tenantname.yourapp.com)
        // Works for both authenticated and unauthenticated requests
        if (string.IsNullOrWhiteSpace(resolvedTenantId))
        {
            var host = context.Request.Host.Host;
            var subdomain = ExtractSubdomain(host);
            
            if (!string.IsNullOrWhiteSpace(subdomain))
            {
                resolvedTenantId = subdomain.ToLowerInvariant();
                source = "SUBDOMAIN";
            }
        }

        // Priority 3: X-Tenant-Id header
        if (string.IsNullOrWhiteSpace(resolvedTenantId))
        {
            var headerId = context.Request.Headers["X-Tenant-Id"].ToString();
            if (!string.IsNullOrWhiteSpace(headerId))
            {
                resolvedTenantId = headerId;
                source = "HEADER_X_TENANT_ID";
            }
        }

        // Priority 4: X-Tenant-Domain header
        if (string.IsNullOrWhiteSpace(resolvedTenantId))
        {
            var headerDomain = context.Request.Headers["X-Tenant-Domain"].ToString();
            if (!string.IsNullOrWhiteSpace(headerDomain))
            {
                resolvedTenantId = headerDomain.ToLowerInvariant();
                source = "HEADER_X_TENANT_DOMAIN";
            }
        }

        // Priority 5: Default fallback
        if (string.IsNullOrWhiteSpace(resolvedTenantId))
        {
            resolvedTenantId = "default";
            source = "DEFAULT";
        }

        // Enforce tenant boundary if authenticated
        // If a tenant is explicitly requested via headers/subdomain and it conflicts with JWT, forbid
        if (isAuthenticated)
        {
            var jwtTenant = context.User.FindFirst("TenantId")?.Value 
                ?? context.User.FindFirst("tenant")?.Value;
            // Explicit tenant requested via header
            var requestedHeaderTenant = context.Request.Headers["X-Tenant-Id"].ToString();

            // If JWT has a tenant claim, it must match either the already-resolved tenant or the explicit header
            if (!string.IsNullOrWhiteSpace(jwtTenant))
            {
                // If there is an explicit header and it conflicts with JWT, forbid
                if (!string.IsNullOrWhiteSpace(requestedHeaderTenant) &&
                    !string.Equals(jwtTenant, requestedHeaderTenant, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync(
                        $"Tenant mismatch. JWT tenant: '{jwtTenant}', Requested tenant: '{requestedHeaderTenant}'. " +
                        "Your authentication token is tied to a different tenant.");
                    return;
                }

                // If resolved tenant (from other source) conflicts with JWT, forbid
                if (!string.IsNullOrWhiteSpace(resolvedTenantId) && source != "JWT_CLAIM" &&
                    !string.Equals(jwtTenant, resolvedTenantId, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync(
                        $"Tenant mismatch. JWT tenant: '{jwtTenant}', Resolved tenant: '{resolvedTenantId}'. " +
                        "Your authentication token is tied to a different tenant.");
                    return;
                }

                // Use JWT as most authoritative source
                resolvedTenantId = jwtTenant;
                source = "JWT_CLAIM";
            }
        }

        // Store resolved tenant ID in context for use by ITenantProvider
        context.Items["TenantId"] = resolvedTenantId;

        await _next(context);
    }

    /// <summary>
    /// Extracts subdomain from hostname.
    /// Examples:
    /// - "tenantname.yourapp.com" -> "tenantname"
    /// - "acme.localhost" -> "acme"
    /// - "yourapp.com" -> null (no subdomain)
    /// </summary>
    private static string? ExtractSubdomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        // Handle localhost for development
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Split by dots
        var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        
        // Need at least 2 parts for a subdomain (subdomain.domain or subdomain.domain.tld)
        if (parts.Length < 2)
            return null;

        // For "tenantname.yourapp.com", the first part is the subdomain
        // For "tenantname.localhost", the first part is also the subdomain
        var subdomain = parts[0].Trim().ToLowerInvariant();

        // Exclude common non-tenant subdomains
        var excludedSubdomains = new[] { "www", "api", "app", "admin", "www" };
        if (excludedSubdomains.Contains(subdomain))
            return null;

        // Validate subdomain format (alphanumeric and hyphens only)
        if (!System.Text.RegularExpressions.Regex.IsMatch(subdomain, @"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$"))
            return null;

        return subdomain;
    }
}


