using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
[Authorize(Roles = "SuperAdmin")]
public class AlertsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAdminAuditService _adminAuditService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        ApplicationDbContext context,
        IAdminAuditService adminAuditService,
        ILogger<AlertsController> logger)
    {
        _context = context;
        _adminAuditService = adminAuditService;
        _logger = logger;
    }

    /// <summary>
    /// Get all alerts with filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? severity = null,
        [FromQuery] int? tenantId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
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

            return Ok(new
            {
                totalCount = totalCount,
                pageNumber = pageNumber,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                alerts = alertsWithMetadata
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching alerts");
            return StatusCode(500, new { message = "Error fetching alerts", error = ex.Message });
        }
    }

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(int id, [FromBody] AcknowledgeAlertRequest? request = null)
    {
        try
        {
            var alert = await _context.Alerts
                .Include(a => a.Tenant)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (alert == null)
            {
                return NotFound(new { message = "Alert not found" });
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

            return Ok(new { message = "Alert acknowledged successfully", alert = new { id = alert.Id, status = alert.Status } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", id);
            return StatusCode(500, new { message = "Error acknowledging alert", error = ex.Message });
        }
    }

    /// <summary>
    /// Resolve an alert
    /// </summary>
    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> ResolveAlert(int id, [FromBody] ResolveAlertRequest? request = null)
    {
        try
        {
            var alert = await _context.Alerts
                .Include(a => a.Tenant)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (alert == null)
            {
                return NotFound(new { message = "Alert not found" });
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

            return Ok(new { message = "Alert resolved successfully", alert = new { id = alert.Id, status = alert.Status, resolvedAt = alert.ResolvedAt } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert {AlertId}", id);
            return StatusCode(500, new { message = "Error resolving alert", error = ex.Message });
        }
    }

    /// <summary>
    /// Get alert statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetAlertStatistics()
    {
        try
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

            return Ok(new
            {
                totalAlerts = totalAlerts,
                activeAlerts = activeAlerts,
                highSeverityAlerts = highSeverityAlerts,
                paymentFailures = paymentFailures,
                resolvedAlerts = resolvedAlerts,
                alertsByType = alertsByType,
                alertsBySeverity = alertsBySeverity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching alert statistics");
            return StatusCode(500, new { message = "Error fetching alert statistics", error = ex.Message });
        }
    }

    public record AcknowledgeAlertRequest(string? Notes = null);
    public record ResolveAlertRequest(string? Notes = null);
}

