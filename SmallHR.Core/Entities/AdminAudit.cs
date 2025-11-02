namespace SmallHR.Core.Entities;

/// <summary>
/// Audit log for all SuperAdmin actions
/// Records every action taken by SuperAdmin users for security and compliance
/// </summary>
public class AdminAudit : BaseEntity
{
    /// <summary>
    /// SuperAdmin user ID who performed the action
    /// </summary>
    public required string AdminUserId { get; set; }

    /// <summary>
    /// SuperAdmin user email (for easier querying)
    /// </summary>
    public required string AdminEmail { get; set; }

    /// <summary>
    /// Action type (e.g., "CreateUser", "UpdateTenant", "DeleteEmployee", etc.)
    /// </summary>
    public required string ActionType { get; set; }

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, etc.)
    /// </summary>
    public required string HttpMethod { get; set; }

    /// <summary>
    /// API endpoint path
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// Target tenant ID (if applicable)
    /// </summary>
    public string? TargetTenantId { get; set; }

    /// <summary>
    /// Target entity type (e.g., "User", "Tenant", "Employee")
    /// </summary>
    public string? TargetEntityType { get; set; }

    /// <summary>
    /// Target entity ID (if applicable)
    /// </summary>
    public string? TargetEntityId { get; set; }

    /// <summary>
    /// Request payload (serialized JSON - sensitive data should be masked)
    /// </summary>
    public string? RequestPayload { get; set; }

    /// <summary>
    /// Response status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// IP address of the request
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Success or failure
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Error message (if action failed)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }
}

