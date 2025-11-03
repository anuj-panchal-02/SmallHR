using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.DTOs.Subscription;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionsController> logger) : base(logger)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Get current tenant's subscription
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<SubscriptionDto>> GetCurrentSubscription()
    {
        // Get tenant ID from context (would need to be set from tenant provider)
        // For now, this is a placeholder - you'd need to get tenantId from authenticated user
        return BadRequest("Tenant ID resolution required");
    }

    /// <summary>
    /// Get subscription by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [AuthorizeAdmin]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(int id)
    {
        return await HandleServiceResultOrNotFoundAsync(
            () => _subscriptionService.GetSubscriptionByIdAsync(id),
            $"getting subscription with ID {id}",
            "Subscription"
        );
    }

    /// <summary>
    /// Get subscription by tenant ID (Admin only)
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    [AuthorizeAdmin]
    public async Task<ActionResult<SubscriptionDto>> GetSubscriptionByTenant(int tenantId)
    {
        return await HandleServiceResultOrNotFoundAsync(
            () => _subscriptionService.GetSubscriptionByTenantIdAsync(tenantId),
            $"getting subscription for tenant {tenantId}",
            "Subscription"
        );
    }

    /// <summary>
    /// Create new subscription (SuperAdmin only)
    /// </summary>
    [HttpPost]
    [AuthorizeSuperAdmin]
    public async Task<ActionResult<SubscriptionDto>> CreateSubscription(CreateSubscriptionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleCreateResultAsync(
            () => _subscriptionService.CreateSubscriptionAsync(request),
            nameof(GetSubscription),
            (s) => s.Id,
            "creating subscription"
        );
    }

    /// <summary>
    /// Update subscription (Admin/SuperAdmin only)
    /// </summary>
    [HttpPut("{id}")]
    [AuthorizeAdmin]
    public async Task<ActionResult<SubscriptionDto>> UpdateSubscription(int id, UpdateSubscriptionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleUpdateResultAsync(
            async () => await _subscriptionService.GetSubscriptionByIdAsync(id) != null,
            async () => await _subscriptionService.UpdateSubscriptionAsync(id, request),
            id,
            "updating subscription",
            "Subscription"
        );
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    [HttpPost("{id}/cancel")]
    [AuthorizeAdmin]
    public async Task<ActionResult<object>> CancelSubscription(int id, [FromQuery] string? reason = null)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var result = await _subscriptionService.CancelSubscriptionAsync(id, reason);
                if (!result)
                {
                    throw new KeyNotFoundException("Subscription not found");
                }
                return new { message = "Subscription canceled successfully" };
            },
            "canceling subscription"
        );
    }

    /// <summary>
    /// Reactivate subscription
    /// </summary>
    [HttpPost("{id}/reactivate")]
    [AuthorizeAdmin]
    public async Task<ActionResult<object>> ReactivateSubscription(int id)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var result = await _subscriptionService.ReactivateSubscriptionAsync(id);
                if (!result)
                {
                    throw new KeyNotFoundException("Subscription not found");
                }
                return new { message = "Subscription reactivated successfully" };
            },
            "reactivating subscription"
        );
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SubscriptionPlanDto>>> GetPlans()
    {
        return await HandleServiceResultAsync(
            () => _subscriptionService.GetAvailablePlansAsync(),
            "getting subscription plans"
        );
    }

    /// <summary>
    /// Get subscription plan by ID
    /// </summary>
    [HttpGet("plans/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<SubscriptionPlanDto>> GetPlan(int id)
    {
        return await HandleServiceResultOrNotFoundAsync(
            () => _subscriptionService.GetPlanByIdAsync(id),
            $"getting subscription plan with ID {id}",
            "SubscriptionPlan"
        );
    }

    /// <summary>
    /// Check if tenant has a feature
    /// </summary>
    [HttpGet("features/{featureKey}")]
    [Authorize]
    public async Task<ActionResult<bool>> HasFeature(string featureKey)
    {
        // Placeholder - would need tenant ID from context
        return BadRequest("Tenant ID resolution required");
    }

    /// <summary>
    /// Get tenant's features
    /// </summary>
    [HttpGet("features")]
    [Authorize]
    public async Task<ActionResult<Dictionary<string, string>>> GetFeatures()
    {
        // Placeholder - would need tenant ID from context
        return BadRequest("Tenant ID resolution required");
    }
}

