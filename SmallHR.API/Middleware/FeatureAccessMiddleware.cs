using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Extensions;

namespace SmallHR.API.Middleware;

/// <summary>
/// Middleware to check subscription plan features before allowing API calls
/// This is a global check - use RequireFeatureAttribute for endpoint-specific checks
/// </summary>
public class FeatureAccessMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FeatureAccessMiddleware> _logger;

    public FeatureAccessMiddleware(
        RequestDelegate next,
        ILogger<FeatureAccessMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantProvider tenantProvider,
        ISubscriptionService subscriptionService,
        ApplicationDbContext dbContext)
    {
        // SuperAdmin bypasses feature checks - operates at platform layer
        if (context.User?.IsSuperAdmin() == true)
        {
            await _next(context);
            return;
        }

        // Skip feature checks for certain paths
        if (ShouldSkipFeatureCheck(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            // Get tenant ID from context
            var tenantIdString = tenantProvider.TenantId;
            if (string.IsNullOrWhiteSpace(tenantIdString) || tenantIdString == "default")
            {
                // No tenant context, allow request (handled by other middleware)
                await _next(context);
                return;
            }

            // Lookup tenant by ID string (domain/name) to get numeric ID
            var tenant = await dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Domain == tenantIdString || t.Name.ToLower() == tenantIdString.ToLower());
            
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found: {TenantId}", tenantIdString);
                await _next(context);
                return; // Let other middleware handle this
            }

            var tenantId = tenant.Id;

            // Get subscription for tenant
            var subscription = await subscriptionService.GetSubscriptionByTenantIdAsync(tenantId);
            if (subscription == null)
            {
                // No subscription - log but allow request (subscription might be created later)
                _logger.LogWarning("No subscription found for tenant: {TenantId} ({TenantName})", tenantId, tenant.Name);
                await _next(context);
                return;
            }

            // Check subscription status (DTO has string status)
            if (!Enum.TryParse<SubscriptionStatus>(subscription.Status, out var subscriptionStatus) ||
                subscriptionStatus != SubscriptionStatus.Active)
            {
                // Inactive subscription - check if request is for activation or billing
                if (context.Request.Path.StartsWithSegments("/api/subscriptions") ||
                    context.Request.Path.StartsWithSegments("/api/billing"))
                {
                    // Allow subscription/billing endpoints
                    await _next(context);
                    return;
                }

                _logger.LogWarning(
                    "Subscription inactive: Tenant {TenantId} ({TenantName}) has subscription status {Status}",
                    tenantId, tenant.Name, subscription.Status);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = "Subscription inactive",
                        message = $"Your subscription is {subscription.Status}. Please activate your subscription to access this feature.",
                        subscriptionStatus = subscription.Status.ToString()
                    }),
                    System.Text.Encoding.UTF8);
                return;
            }

            // Store subscription info in context for use by RequireFeatureAttribute
            context.Items["TenantId"] = tenantId;
            context.Items["SubscriptionId"] = subscription.Id;
            context.Items["SubscriptionPlanId"] = subscription.SubscriptionPlanId;

            // Continue to next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in feature access middleware: {Message}", ex.Message);
            // On error, allow request to continue (fail open for availability)
            await _next(context);
        }
    }

    private bool ShouldSkipFeatureCheck(PathString path)
    {
        // Skip feature checks for:
        // - Health checks
        // - Authentication endpoints
        // - Subscription/billing endpoints (for subscription management)
        // - Swagger documentation
        // - Development endpoints
        var skipPaths = new[]
        {
            "/health",
            "/api/auth",
            "/api/subscriptions",
            "/api/billing",
            "/swagger",
            "/api/dev",
            "/api/webhooks"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath));
    }
}

