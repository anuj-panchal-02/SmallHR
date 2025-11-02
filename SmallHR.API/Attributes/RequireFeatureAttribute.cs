using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.API.Attributes;

/// <summary>
/// Attribute to mark endpoints that require specific subscription plan features
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireFeatureAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _requiredFeatures;

    public RequireFeatureAttribute(params string[] requiredFeatures)
    {
        _requiredFeatures = requiredFeatures ?? throw new ArgumentNullException(nameof(requiredFeatures));
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get services from DI
        var subscriptionService = context.HttpContext.RequestServices.GetRequiredService<ISubscriptionService>();
        var tenantProvider = context.HttpContext.RequestServices.GetRequiredService<ITenantProvider>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireFeatureAttribute>>();
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Get tenant ID from context
            var tenantIdString = tenantProvider.TenantId;
            if (string.IsNullOrWhiteSpace(tenantIdString) || tenantIdString == "default")
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "Unauthorized",
                    message = "Invalid tenant context"
                });
                return;
            }

            // Lookup tenant by ID string (domain/name) to get numeric ID
            var tenant = await dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Domain == tenantIdString || t.Name.ToLower() == tenantIdString.ToLower());
            
            if (tenant == null)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "Tenant not found",
                    message = $"Tenant '{tenantIdString}' not found"
                });
                return;
            }

            var tenantId = tenant.Id;
            
            // Get subscription for tenant
            var subscription = await subscriptionService.GetSubscriptionByTenantIdAsync(tenantId);
            if (subscription == null)
            {
                context.Result = new ForbidObjectResult(new
                {
                    error = "Subscription required",
                    message = "No active subscription found for this tenant"
                });
                return;
            }

            // Check subscription status (DTO has string status)
            if (!Enum.TryParse<SubscriptionStatus>(subscription.Status, out var subscriptionStatus) ||
                subscriptionStatus != SubscriptionStatus.Active)
            {
                context.Result = new ForbidObjectResult(new
                {
                    error = "Subscription inactive",
                    message = $"Subscription is {subscription.Status}. Please activate your subscription."
                });
                return;
            }

            // Get subscription plan
            var plan = await subscriptionService.GetPlanByIdAsync(subscription.SubscriptionPlanId);
            if (plan == null)
            {
                context.Result = new ForbidObjectResult(new
                {
                    error = "Invalid plan",
                    message = "Subscription plan not found"
                });
                return;
            }

            // Check each required feature
            foreach (var featureKey in _requiredFeatures)
            {
                var hasFeature = await subscriptionService.HasFeatureAsync(tenantId, featureKey);
                if (!hasFeature)
                {
                    logger.LogWarning(
                        "Feature access denied: Tenant {TenantId} ({TenantName}) attempted to access feature {Feature} which is not available in plan {PlanName}",
                        tenantId, tenant.Name, featureKey, plan.Name);

                    context.Result = new ForbidObjectResult(new
                    {
                        error = "Feature not available",
                        message = $"Feature '{featureKey}' is not available in your current plan ({plan.Name}). Please upgrade to access this feature.",
                        requiredFeature = featureKey,
                        currentPlan = plan.Name
                    });
                    return;
                }
            }

            // All features available, allow request
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking feature access: {Message}", ex.Message);
            
            // On error, deny access (fail closed)
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

// Helper class for ForbidObjectResult
public class ForbidObjectResult : ObjectResult
{
    public ForbidObjectResult(object value) : base(value)
    {
        StatusCode = StatusCodes.Status403Forbidden;
    }
}

