namespace SmallHR.Core.Interfaces;

/// <summary>
/// Service for logging SuperAdmin actions to AdminAudit
/// </summary>
public interface IAdminAuditService
{
    /// <summary>
    /// Log a SuperAdmin action
    /// </summary>
    Task LogActionAsync(
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
        long? durationMs = null);
}

