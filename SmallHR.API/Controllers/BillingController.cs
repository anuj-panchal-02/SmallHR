using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using System.Security.Claims;

namespace SmallHR.API.Controllers;

/// <summary>
/// Billing Administration for SuperAdmin
/// Webhook events management and reconciliation
/// </summary>
[ApiController]
[Route("api/admin/billing")]
[Authorize(Roles = "SuperAdmin")]
public class BillingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAdminAuditService _adminAuditService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        ApplicationDbContext context,
        IAdminAuditService adminAuditService,
        ILogger<BillingController> logger)
    {
        _context = context;
        _adminAuditService = adminAuditService;
        _logger = logger;
    }

    /// <summary>
    /// Get webhook events with filters
    /// </summary>
    [HttpGet("webhooks")]
    public async Task<IActionResult> GetWebhookEvents(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null,
        [FromQuery] string? provider = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.WebhookEvents.AsQueryable();

            // Date range filter
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;
            query = query.Where(w => w.CreatedAt >= start && w.CreatedAt <= end);

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                var isProcessed = status.ToLower() == "processed";
                query = query.Where(w => w.Processed == isProcessed);
            }

            // Provider filter
            if (!string.IsNullOrWhiteSpace(provider))
            {
                query = query.Where(w => w.Provider == provider);
            }

            var totalCount = await query.CountAsync();
            var webhooks = await query
                .Include(w => w.Tenant)
                .Include(w => w.Subscription)
                .OrderByDescending(w => w.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                totalCount = totalCount,
                pageNumber = pageNumber,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                webhooks = webhooks.Select(w => new
                {
                    id = w.Id,
                    eventType = w.EventType,
                    provider = w.Provider,
                    status = w.Processed ? "Processed" : (w.Error != null ? "Failed" : "Pending"),
                    processed = w.Processed,
                    tenantId = w.TenantId,
                    tenantName = w.Tenant?.Name,
                    subscriptionId = w.SubscriptionId,
                    createdAt = w.CreatedAt,
                    error = w.Error
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching webhook events");
            return StatusCode(500, new { message = "Error fetching webhook events", error = ex.Message });
        }
    }

    /// <summary>
    /// Reconcile billing data (compare subscriptions with webhook events)
    /// </summary>
    [HttpPost("reconcile")]
    public async Task<IActionResult> Reconcile([FromBody] ReconcileRequest? request = null)
    {
        try
        {
            var startDate = request?.StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = request?.EndDate ?? DateTime.UtcNow;

            var adminUser = HttpContext.User.FindFirst(ClaimTypes.Email);
            var adminUserId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

            // Get all subscriptions in the date range
            var subscriptions = await _context.Subscriptions
                .Include(s => s.Tenant)
                .Include(s => s.Plan)
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                .ToListAsync();

            var reconciled = 0;
            var discrepancies = 0;
            var discrepancyDetails = new List<object>();

            foreach (var subscription in subscriptions)
            {
                // Check if webhook events exist for this subscription
                var webhookEvents = await _context.WebhookEvents
                    .Where(w => w.SubscriptionId == subscription.Id || 
                               (w.TenantId == subscription.TenantId && w.Provider == subscription.BillingProvider.ToString()))
                    .ToListAsync();

                if (webhookEvents.Any())
                {
                    reconciled++;
                }
                else
                {
                    discrepancies++;
                    discrepancyDetails.Add(new
                    {
                        subscriptionId = subscription.Id,
                        tenantId = subscription.TenantId,
                        tenantName = subscription.Tenant?.Name,
                        planName = subscription.Plan?.Name,
                        status = subscription.Status.ToString(),
                        externalSubscriptionId = subscription.ExternalSubscriptionId,
                        billingProvider = subscription.BillingProvider.ToString(),
                        issue = "No webhook events found for this subscription"
                    });
                }
            }

            // Log action
            await _adminAuditService.LogActionAsync(
                adminUserId: adminUserId,
                adminEmail: adminUser?.Value ?? "unknown",
                actionType: "Billing.Reconcile",
                httpMethod: "POST",
                endpoint: "/api/admin/billing/reconcile",
                statusCode: 200,
                isSuccess: true,
                requestPayload: request != null ? System.Text.Json.JsonSerializer.Serialize(request) : null,
                metadata: $"{{\"reconciled\": {reconciled}, \"discrepancies\": {discrepancies}, \"totalChecked\": {subscriptions.Count}}}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

            return Ok(new
            {
                reconciled = reconciled,
                discrepancies = discrepancies,
                totalChecked = subscriptions.Count,
                discrepancyDetails = discrepancyDetails
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconciling billing data");
            return StatusCode(500, new { message = "Error reconciling billing data", error = ex.Message });
        }
    }

    public record ReconcileRequest(DateTime? StartDate = null, DateTime? EndDate = null);
}

