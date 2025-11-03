using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using System.Security.Claims;
using System.Text.Json;

namespace SmallHR.API.Controllers;

/// <summary>
/// Alerts Management for SuperAdmin
/// Manage system alerts (payment failures, overages, errors, suspensions)
/// </summary>
[ApiController]
[Route("api/admin/alerts")]
[AuthorizeSuperAdmin]
public class AlertsController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IAdminAuditService _adminAuditService;

    public AlertsController(
        ApplicationDbContext context,
        IAdminAuditService adminAuditService,
        ILogger<AlertsController> logger) : base(logger)
    {
        _context = context;
        _adminAuditService = adminAuditService;
    }

    /// <summary>
    /// Get all alerts with filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetAlerts(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? severity = null,
        [FromQuery] int? tenantId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var query = _context.Alerts
                    .Include(a => a.Tenant)
                    .Include(a => a.Subscription)
                        .ThenInclude(s => s!.Plan)
                    .AsQueryable();

                // Status filter
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(a => a.Status == status);
                }

                // Type filter
                if (!string.IsNullOrWhiteSpace(type))
                {
                    query = query.Where(a => a.AlertType == type);
                }

                // Severity filter
                if (!string.IsNullOrWhiteSpace(severity))
                {
                    query = query.Where(a => a.Severity == severity);
                }

                // Tenant filter
                if (tenantId.HasValue)
                {
                    query = query.Where(a => a.TenantId == tenantId.Value);
                }

                var totalCount = await query.CountAsync();
                var alerts = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Parse metadata JSON for each alert
                var alertsWithMetadata = alerts.Select(a =>
                {
                    Dictionary<string, object>? metadata = null;
                    if (!string.IsNullOrWhiteSpace(a.MetadataJson))
                    {
                        try
                        {
                            metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(a.MetadataJson);
                        }
                        catch
                        {
                            // Ignore JSON parsing errors
                        }
                    }

                    return new
                    {
                        id = a.Id,
                        tenantId = a.TenantId,
                        tenantName = a.Tenant.Name,
                        alertType = a.AlertType,
                        severity = a.Severity,
                        message = a.Message,
                        status = a.Status,
                        createdAt = a.CreatedAt,
                        resolvedAt = a.ResolvedAt,
                        resolvedBy = a.ResolvedBy,
                        resolutionNotes = a.ResolutionNotes,
                        subscriptionId = a.SubscriptionId,
                        subscriptionPlanName = a.Subscription?.Plan?.Name,
                        metadata = metadata
                    };
                });

                return new
                {
                    totalCount = totalCount,
                    pageNumber = pageNumber,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    alerts = alertsWithMetadata
                };
            },
            "fetching alerts"
        );
    }

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    public async Task<ActionResult<object>> AcknowledgeAlert(int id, [FromBody] AcknowledgeAlertRequest? request = null)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var alert = await _context.Alerts
                    .Include(a => a.Tenant)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (alert == null)
                {
                    throw new KeyNotFoundException("Alert not found");
                }

                var adminUser = HttpContext.User.FindFirst(ClaimTypes.Email);
                var adminUserId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

                alert.Status = "Acknowledged";
                alert.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Log action
                await _adminAuditService.LogActionAsync(
                    adminUserId: adminUserId,
                    adminEmail: adminUser?.Value ?? "unknown",
                    actionType: "Alert.Acknowledge",
                    httpMethod: "POST",
                    endpoint: $"/api/admin/alerts/{id}/acknowledge",
                    statusCode: 200,
                    isSuccess: true,
                    targetTenantId: alert.TenantId.ToString(),
                    targetEntityType: "Alert",
                    targetEntityId: alert.Id.ToString(),
                    requestPayload: request != null ? JsonSerializer.Serialize(request) : null,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

                return new { message = "Alert acknowledged successfully", alert = new { id = alert.Id, status = alert.Status } };
            },
            "acknowledging alert"
        );
    }

    /// <summary>
    /// Resolve an alert
    /// </summary>
    [HttpPost("{id}/resolve")]
    public async Task<ActionResult<object>> ResolveAlert(int id, [FromBody] ResolveAlertRequest? request = null)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var alert = await _context.Alerts
                    .Include(a => a.Tenant)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (alert == null)
                {
                    throw new KeyNotFoundException("Alert not found");
                }

                var adminUser = HttpContext.User.FindFirst(ClaimTypes.Email);
                var adminUserId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

                alert.Status = "Resolved";
                alert.ResolvedAt = DateTime.UtcNow;
                alert.ResolvedBy = adminUserId;
                alert.ResolutionNotes = request?.Notes;
                alert.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Log action
                await _adminAuditService.LogActionAsync(
                    adminUserId: adminUserId,
                    adminEmail: adminUser?.Value ?? "unknown",
                    actionType: "Alert.Resolve",
                    httpMethod: "POST",
                    endpoint: $"/api/admin/alerts/{id}/resolve",
                    statusCode: 200,
                    isSuccess: true,
                    targetTenantId: alert.TenantId.ToString(),
                    targetEntityType: "Alert",
                    targetEntityId: alert.Id.ToString(),
                    requestPayload: request != null ? JsonSerializer.Serialize(request) : null,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

                return new { message = "Alert resolved successfully", alert = new { id = alert.Id, status = alert.Status, resolvedAt = alert.ResolvedAt } };
            },
            "resolving alert"
        );
    }

    /// <summary>
    /// Get alert statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetAlertStatistics()
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var totalAlerts = await _context.Alerts.CountAsync();
                var activeAlerts = await _context.Alerts.CountAsync(a => a.Status == "Active");
                var highSeverityAlerts = await _context.Alerts.CountAsync(a => a.Severity == "High" && a.Status == "Active");
                var paymentFailures = await _context.Alerts.CountAsync(a => a.AlertType == "PaymentFailure" && a.Status == "Active");
                var resolvedAlerts = await _context.Alerts.CountAsync(a => a.Status == "Resolved");

                var alertsByType = await _context.Alerts
                    .Where(a => a.Status == "Active")
                    .GroupBy(a => a.AlertType)
                    .Select(g => new { type = g.Key, count = g.Count() })
                    .ToListAsync();

                var alertsBySeverity = await _context.Alerts
                    .Where(a => a.Status == "Active")
                    .GroupBy(a => a.Severity)
                    .Select(g => new { severity = g.Key, count = g.Count() })
                    .ToListAsync();

                return new
                {
                    totalAlerts = totalAlerts,
                    activeAlerts = activeAlerts,
                    highSeverityAlerts = highSeverityAlerts,
                    paymentFailures = paymentFailures,
                    resolvedAlerts = resolvedAlerts,
                    alertsByType = alertsByType,
                    alertsBySeverity = alertsBySeverity
                };
            },
            "fetching alert statistics"
        );
    }

    public record AcknowledgeAlertRequest(string? Notes = null);
    public record ResolveAlertRequest(string? Notes = null);
}

