using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SmallHR.Core.Interfaces;
using SmallHR.API.Extensions;

namespace SmallHR.API.Middleware;

/// <summary>
/// Middleware to audit all SuperAdmin actions
/// Logs every action taken by SuperAdmin users to AdminAudit table
/// </summary>
public class AdminAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminAuditMiddleware> _logger;

    public AdminAuditMiddleware(
        RequestDelegate next,
        ILogger<AdminAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAdminAuditService adminAuditService)
    {
        // Check if user is SuperAdmin
        var isSuperAdmin = context.User?.IsInRole("SuperAdmin") == true;

        if (!isSuperAdmin)
        {
            // Not SuperAdmin, continue without audit
            await _next(context);
            return;
        }

        // SuperAdmin action - start timer and capture request details
        var stopwatch = Stopwatch.StartNew();
        var adminUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var adminEmail = context.User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";
        var httpMethod = context.Request.Method;
        var endpoint = context.Request.Path + context.Request.QueryString;
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        // Capture request body (only for POST/PUT/PATCH)
        string? requestPayload = null;
        if (context.Request.Method is "POST" or "PUT" or "PATCH")
        {
            context.Request.EnableBuffering();
            var bodyStream = context.Request.Body;
            bodyStream.Position = 0;
            using var reader = new StreamReader(bodyStream, Encoding.UTF8, leaveOpen: true);
            requestPayload = await reader.ReadToEndAsync();
            bodyStream.Position = 0;

            // Mask sensitive fields (passwords, tokens, etc.)
            requestPayload = MaskSensitiveData(requestPayload);
        }

        // Store original response body stream
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            // Execute the request
            await _next(context);

            // Capture response details
            var statusCode = context.Response.StatusCode;
            var isSuccess = statusCode >= 200 && statusCode < 300;
            stopwatch.Stop();

            // Extract target information from response headers (if set by controller)
            var targetTenantId = context.Response.Headers["X-Target-TenantId"].ToString();
            var targetEntityType = context.Response.Headers["X-Target-EntityType"].ToString();
            var targetEntityId = context.Response.Headers["X-Target-EntityId"].ToString();

            // Determine action type from route
            var actionType = DetermineActionType(httpMethod, endpoint);

            // Log the action
            await adminAuditService.LogActionAsync(
                adminUserId: adminUserId,
                adminEmail: adminEmail,
                actionType: actionType,
                httpMethod: httpMethod,
                endpoint: endpoint,
                statusCode: statusCode,
                isSuccess: isSuccess,
                targetTenantId: string.IsNullOrWhiteSpace(targetTenantId) ? null : targetTenantId,
                targetEntityType: string.IsNullOrWhiteSpace(targetEntityType) ? null : targetEntityType,
                targetEntityId: string.IsNullOrWhiteSpace(targetEntityId) ? null : targetEntityId,
                requestPayload: requestPayload,
                ipAddress: ipAddress,
                userAgent: userAgent,
                durationMs: stopwatch.ElapsedMilliseconds);

            // Copy response body back
            responseBody.Position = 0;
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode > 0 ? context.Response.StatusCode : 500;

            // Log failed action
            try
            {
                await adminAuditService.LogActionAsync(
                    adminUserId: adminUserId,
                    adminEmail: adminEmail,
                    actionType: DetermineActionType(httpMethod, endpoint),
                    httpMethod: httpMethod,
                    endpoint: endpoint,
                    statusCode: statusCode,
                    isSuccess: false,
                    requestPayload: requestPayload,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    errorMessage: ex.Message,
                    durationMs: stopwatch.ElapsedMilliseconds);
            }
            catch (Exception auditEx)
            {
                // Don't fail the request if audit logging fails
                _logger.LogError(auditEx, "Failed to log admin audit for failed request");
            }

            // Copy response body back
            if (context.Response.Body != originalBodyStream)
            {
                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBodyStream);
            }

            // Re-throw the exception to maintain error handling
            throw;
        }
        finally
        {
            if (context.Response.Body != originalBodyStream)
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }

    private static string DetermineActionType(string httpMethod, string endpoint)
    {
        // Extract action type from endpoint
        // e.g., /api/usermanagement/users -> "UserManagement.GetAll"
        // e.g., /api/tenants/123 -> "Tenant.GetById"
        var parts = endpoint.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var controller = parts[1].Replace("api/", "").Replace("controller", "");
            var action = httpMethod switch
            {
                "GET" => parts.Length > 2 ? "GetById" : "GetAll",
                "POST" => "Create",
                "PUT" => "Update",
                "PATCH" => "Update",
                "DELETE" => "Delete",
                _ => httpMethod
            };
            return $"{controller}.{action}";
        }
        return $"{httpMethod}.{endpoint}";
    }

    private static string MaskSensitiveData(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return payload;

        // Mask passwords
        payload = System.Text.RegularExpressions.Regex.Replace(
            payload,
            @"""password""\s*:\s*""[^""]*""",
            "\"password\":\"***MASKED***\"",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Mask tokens
        payload = System.Text.RegularExpressions.Regex.Replace(
            payload,
            @"""token""\s*:\s*""[^""]*""",
            "\"token\":\"***MASKED***\"",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Mask refresh tokens
        payload = System.Text.RegularExpressions.Regex.Replace(
            payload,
            @"""refreshToken""\s*:\s*""[^""]*""",
            "\"refreshToken\":\"***MASKED***\"",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return payload;
    }
}

