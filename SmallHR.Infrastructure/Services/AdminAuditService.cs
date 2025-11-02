using Microsoft.Extensions.Logging;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Services;

public class AdminAuditService : IAdminAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminAuditService> _logger;

    public AdminAuditService(
        ApplicationDbContext context,
        ILogger<AdminAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActionAsync(
        string adminUserId,
        string adminEmail,
        string actionType,
        string httpMethod,
        string endpoint,
        int statusCode,
        bool isSuccess,
        string? targetTenantId = null,
        string? targetEntityType = null,
        string? targetEntityId = null,
        string? requestPayload = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null,
        string? errorMessage = null,
        long? durationMs = null)
    {
        try
        {
            // Truncate request payload if too long (mask sensitive data if needed)
            var truncatedPayload = requestPayload;
            if (!string.IsNullOrEmpty(truncatedPayload) && truncatedPayload.Length > 4000)
            {
                truncatedPayload = truncatedPayload.Substring(0, 4000) + "... [truncated]";
            }

            var audit = new AdminAudit
            {
                AdminUserId = adminUserId,
                AdminEmail = adminEmail,
                ActionType = actionType,
                HttpMethod = httpMethod,
                Endpoint = endpoint,
                StatusCode = statusCode,
                IsSuccess = isSuccess,
                TargetTenantId = targetTenantId,
                TargetEntityType = targetEntityType,
                TargetEntityId = targetEntityId,
                RequestPayload = truncatedPayload,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Metadata = metadata,
                ErrorMessage = errorMessage,
                DurationMs = durationMs,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminAudits.Add(audit);
            await _context.SaveChangesAsync();

            _logger.LogDebug(
                "AdminAudit logged: {ActionType} by {AdminEmail} on {Endpoint} - Status: {StatusCode}",
                actionType, adminEmail, endpoint, statusCode);
        }
        catch (Exception ex)
        {
            // Don't fail the request if audit logging fails
            _logger.LogError(ex,
                "Failed to log AdminAudit: {ActionType} by {AdminEmail} on {Endpoint}",
                actionType, adminEmail, endpoint);
        }
    }
}

