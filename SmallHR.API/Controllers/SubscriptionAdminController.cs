using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using System.Security.Claims;

namespace SmallHR.API.Controllers;

/// <summary>
/// Subscription Administration for SuperAdmin
/// Allows manual plan adjustments, trial extensions, upgrades/downgrades
/// </summary>
[ApiController]
[Route("api/admin/subscriptions")]
[AuthorizeSuperAdmin]
public class SubscriptionAdminController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IAdminAuditService _adminAuditService;

    public SubscriptionAdminController(
        ApplicationDbContext context,
        IAdminAuditService adminAuditService,
        ILogger<SubscriptionAdminController> logger) : base(logger)
    {
        _context = context;
        _adminAuditService = adminAuditService;
    }

    /// <summary>
    /// Get subscription history for a tenant
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    public async Task<ActionResult<object>> GetTenantSubscriptions(int tenantId)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var subscriptions = await _context.Subscriptions
                    .Include(s => s.Plan)
                    .Include(s => s.Tenant)
                    .Where(s => s.TenantId == tenantId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return new
                {
                    tenantId = tenantId,
                    subscriptions = subscriptions.Select(s => new
                    {
                        id = s.Id,
                        planName = s.Plan?.Name ?? "Unknown",
                        status = s.Status.ToString(),
                        price = s.Price,
                        currency = s.Currency,
                        billingPeriod = s.BillingPeriod.ToString(),
                        startDate = s.StartDate,
                        endDate = s.EndDate,
                        trialEndDate = s.TrialEndDate,
                        canceledAt = s.CanceledAt,
                        autoRenew = s.AutoRenew,
                        externalSubscriptionId = s.ExternalSubscriptionId,
                        externalCustomerId = s.ExternalCustomerId,
                        billingProvider = s.BillingProvider.ToString(),
                        createdAt = s.CreatedAt,
                        updatedAt = s.UpdatedAt
                    })
                };
            },
            "fetching subscriptions for tenant"
        );
    }

    /// <summary>
    /// Extend trial period for a subscription
    /// </summary>
    [HttpPost("{subscriptionId}/extend-trial")]
    public async Task<ActionResult<object>> ExtendTrial(int subscriptionId, [FromBody] ExtendTrialRequest request)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var subscription = await _context.Subscriptions
                    .Include(s => s.Tenant)
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.Id == subscriptionId);

                if (subscription == null)
                {
                    throw new KeyNotFoundException("Subscription not found");
                }

                var adminUser = HttpContext.User.FindFirst(ClaimTypes.Email);
                var adminUserId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

                // Extend trial
                var newTrialEndDate = subscription.TrialEndDate.HasValue
                    ? subscription.TrialEndDate.Value.AddDays(request.ExtendByDays)
                    : DateTime.UtcNow.AddDays(request.ExtendByDays);

                subscription.TrialEndDate = newTrialEndDate;
                subscription.Notes = $"{subscription.Notes}\nTrial extended by {request.ExtendByDays} days on {DateTime.UtcNow:yyyy-MM-dd} by {adminUser?.Value}";
                
                await _context.SaveChangesAsync();

                // Log action
                await _adminAuditService.LogActionAsync(
                    adminUserId: adminUserId,
                    adminEmail: adminUser?.Value ?? "unknown",
                    actionType: "Subscription.ExtendTrial",
                    httpMethod: "POST",
                    endpoint: $"/api/admin/subscriptions/{subscriptionId}/extend-trial",
                    statusCode: 200,
                    isSuccess: true,
                    targetTenantId: subscription.TenantId.ToString(),
                    targetEntityType: "Subscription",
                    targetEntityId: subscription.Id.ToString(),
                    requestPayload: System.Text.Json.JsonSerializer.Serialize(request),
                    metadata: $"{{\"extendedByDays\": {request.ExtendByDays}, \"newTrialEndDate\": \"{newTrialEndDate:yyyy-MM-dd}\"}}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

                return new
                {
                    message = $"Trial extended by {request.ExtendByDays} days",
                    subscription = new
                    {
                        id = subscription.Id,
                        tenantId = subscription.TenantId,
                        tenantName = subscription.Tenant?.Name,
                        planName = subscription.Plan?.Name,
                        trialEndDate = subscription.TrialEndDate
                    }
                };
            },
            "extending trial for subscription"
        );
    }

    /// <summary>
    /// Upgrade or downgrade a subscription plan
    /// </summary>
    [HttpPost("{subscriptionId}/change-plan")]
    public async Task<ActionResult<object>> ChangePlan(int subscriptionId, [FromBody] ChangePlanRequest request)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var subscription = await _context.Subscriptions
                    .Include(s => s.Tenant)
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.Id == subscriptionId);

                if (subscription == null)
                {
                    throw new KeyNotFoundException("Subscription not found");
                }

                var newPlan = await _context.SubscriptionPlans.FindAsync(request.NewPlanId);
                if (newPlan == null)
                {
                    throw new KeyNotFoundException("Subscription plan not found");
                }

                var adminUser = HttpContext.User.FindFirst(ClaimTypes.Email);
                var adminUserId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

                // Calculate new price based on billing period
                var newPrice = request.BillingPeriod switch
                {
                    BillingPeriod.Monthly => newPlan.MonthlyPrice,
                    BillingPeriod.Quarterly => newPlan.QuarterlyPrice ?? newPlan.MonthlyPrice * 3,
                    BillingPeriod.Yearly => newPlan.YearlyPrice ?? newPlan.MonthlyPrice * 12,
                    _ => newPlan.MonthlyPrice
                };

                var oldPlanId = subscription.SubscriptionPlanId;
                var oldPlanName = subscription.Plan?.Name;

                // Update subscription
                subscription.SubscriptionPlanId = request.NewPlanId;
                subscription.Price = newPrice;
                subscription.BillingPeriod = request.BillingPeriod;
                subscription.Notes = $"{subscription.Notes}\nPlan changed from {oldPlanName} to {newPlan.Name} on {DateTime.UtcNow:yyyy-MM-dd} by {adminUser?.Value}";
                
                await _context.SaveChangesAsync();

                // Log action
                await _adminAuditService.LogActionAsync(
                    adminUserId: adminUserId,
                    adminEmail: adminUser?.Value ?? "unknown",
                    actionType: "Subscription.ChangePlan",
                    httpMethod: "POST",
                    endpoint: $"/api/admin/subscriptions/{subscriptionId}/change-plan",
                    statusCode: 200,
                    isSuccess: true,
                    targetTenantId: subscription.TenantId.ToString(),
                    targetEntityType: "Subscription",
                    targetEntityId: subscription.Id.ToString(),
                    requestPayload: System.Text.Json.JsonSerializer.Serialize(request),
                    metadata: $"{{\"oldPlanId\": {oldPlanId}, \"oldPlanName\": \"{oldPlanName}\", \"newPlanId\": {request.NewPlanId}, \"newPlanName\": \"{newPlan.Name}\"}}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

                return new
                {
                    message = $"Plan changed from {oldPlanName} to {newPlan.Name}",
                    subscription = new
                    {
                        id = subscription.Id,
                        tenantId = subscription.TenantId,
                        tenantName = subscription.Tenant?.Name,
                        oldPlanName = oldPlanName,
                        newPlanName = newPlan.Name,
                        price = subscription.Price,
                        billingPeriod = subscription.BillingPeriod.ToString()
                    }
                };
            },
            "changing plan for subscription"
        );
    }

    public record ExtendTrialRequest(int ExtendByDays, string? Reason = null);
    public record ChangePlanRequest(int NewPlanId, BillingPeriod BillingPeriod, string? Reason = null);
}

