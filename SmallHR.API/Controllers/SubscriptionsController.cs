using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.Core.DTOs.Subscription;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
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
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(int id)
    {
        var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
        if (subscription == null)
            return NotFound();

        return Ok(subscription);
    }

    /// <summary>
    /// Get subscription by tenant ID (Admin only)
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<SubscriptionDto>> GetSubscriptionByTenant(int tenantId)
    {
        var subscription = await _subscriptionService.GetSubscriptionByTenantIdAsync(tenantId);
        if (subscription == null)
            return NotFound();

        return Ok(subscription);
    }

    /// <summary>
    /// Create new subscription (SuperAdmin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<SubscriptionDto>> CreateSubscription(CreateSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.CreateSubscriptionAsync(request);
            return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, subscription);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update subscription (Admin/SuperAdmin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<SubscriptionDto>> UpdateSubscription(int id, UpdateSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.UpdateSubscriptionAsync(id, request);
            return Ok(subscription);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CancelSubscription(int id, [FromQuery] string? reason = null)
    {
        var result = await _subscriptionService.CancelSubscriptionAsync(id, reason);
        if (!result)
            return NotFound();

        return Ok(new { message = "Subscription canceled successfully" });
    }

    /// <summary>
    /// Reactivate subscription
    /// </summary>
    [HttpPost("{id}/reactivate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> ReactivateSubscription(int id)
    {
        var result = await _subscriptionService.ReactivateSubscriptionAsync(id);
        if (!result)
            return NotFound();

        return Ok(new { message = "Subscription reactivated successfully" });
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SubscriptionPlanDto>>> GetPlans()
    {
        var plans = await _subscriptionService.GetAvailablePlansAsync();
        return Ok(plans);
    }

    /// <summary>
    /// Get subscription plan by ID
    /// </summary>
    [HttpGet("plans/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<SubscriptionPlanDto>> GetPlan(int id)
    {
        var plan = await _subscriptionService.GetPlanByIdAsync(id);
        if (plan == null)
            return NotFound();

        return Ok(plan);
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

