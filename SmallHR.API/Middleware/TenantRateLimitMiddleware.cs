using Microsoft.Extensions.Caching.Memory;
using SmallHR.Core.Interfaces;
using System.Text;

namespace SmallHR.API.Middleware;

/// <summary>
/// Tenant-based rate limiting middleware
/// Enforces rate limits per tenant based on subscription plan
/// </summary>
public class TenantRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantRateLimitMiddleware> _logger;
    private readonly IUsageMetricsService _usageMetricsService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IMemoryCache _cache;

    public TenantRateLimitMiddleware(
        RequestDelegate next,
        ILogger<TenantRateLimitMiddleware> logger,
        IUsageMetricsService usageMetricsService,
        ISubscriptionService subscriptionService,
        IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _usageMetricsService = usageMetricsService;
        _subscriptionService = subscriptionService;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        // Skip rate limiting for certain paths
        if (ShouldSkipRateLimit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            // Get tenant ID from provider
            var tenantId = GetTenantIdFromContext(context, tenantProvider);
            if (tenantId == null)
            {
                // No tenant ID, allow request but don't track
                await _next(context);
                return;
            }

            // Get subscription to determine rate limits
            var subscription = await _subscriptionService.GetSubscriptionByTenantIdAsync(tenantId.Value);
            if (subscription == null)
            {
                // No subscription, use default limits
                var defaultLimit = GetDefaultRateLimit();
                if (!await CheckAndEnforceRateLimit(tenantId.Value, context, defaultLimit))
                {
                    return; // Rate limit exceeded, response already sent
                }
            }
            else
            {
                // Get plan-based limits
                var plan = await _subscriptionService.GetPlanByIdAsync(subscription.SubscriptionPlanId);
                var rateLimit = plan != null ? GetRateLimitForPlan(plan.Name) : GetDefaultRateLimit();

                if (!await CheckAndEnforceRateLimit(tenantId.Value, context, rateLimit))
                {
                    return; // Rate limit exceeded, response already sent
                }
            }

            // Increment API request count
            await _usageMetricsService.IncrementApiRequestCountAsync(tenantId.Value);

            // Continue to next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant rate limit middleware: {Message}", ex.Message);
            // On error, allow request to continue (fail open)
            await _next(context);
        }
    }

    private bool ShouldSkipRateLimit(PathString path)
    {
        // Skip rate limiting for:
        // - Health checks
        // - Webhook endpoints (they have their own authentication)
        // - Static files
        var skipPaths = new[]
        {
            "/health",
            "/api/webhooks",
            "/swagger",
            "/api/dev"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath));
    }

    private int? GetTenantIdFromContext(HttpContext context, ITenantProvider tenantProvider)
    {
        // Try to get tenant ID from resolved tenant
        var tenantIdString = tenantProvider.TenantId;

        // For now, we'd need to lookup tenant by name/id
        // This is a placeholder - you'd need to implement tenant lookup
        // For development, we can skip or use a default tenant ID
        
        // TODO: Implement tenant lookup by tenant name/ID
        // For now, return null to allow all requests
        return null;
    }

    private async Task<bool> CheckAndEnforceRateLimit(int tenantId, HttpContext context, RateLimitConfig rateLimit)
    {
        var cacheKey = $"ratelimit:tenant:{tenantId}:{DateTime.UtcNow:yyyy-MM-dd}";
        
        // Get or create rate limit counter
        if (!_cache.TryGetValue(cacheKey, out RateLimitCounter? counter))
        {
            counter = new RateLimitCounter
            {
                TenantId = tenantId,
                Date = DateTime.UtcNow.Date,
                RequestCount = 0,
                ResetTime = DateTime.UtcNow.Date.AddDays(1)
            };
            _cache.Set(cacheKey, counter, counter.ResetTime);
        }

        // Check if limit exceeded
        if (counter.RequestCount >= rateLimit.RequestsPerDay)
        {
            _logger.LogWarning("Rate limit exceeded for tenant {TenantId}: {Count}/{Limit}", 
                tenantId, counter.RequestCount, rateLimit.RequestsPerDay);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                error = "Rate limit exceeded",
                message = $"You have exceeded the daily API request limit ({rateLimit.RequestsPerDay} requests/day). Please upgrade your plan or try again tomorrow.",
                retryAfter = (counter.ResetTime - DateTime.UtcNow).TotalSeconds
            };

            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(response),
                Encoding.UTF8);

            // Add rate limit headers
            context.Response.Headers["X-RateLimit-Limit"] = rateLimit.RequestsPerDay.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, rateLimit.RequestsPerDay - counter.RequestCount).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)counter.ResetTime).ToUnixTimeSeconds().ToString();
            context.Response.Headers["Retry-After"] = ((int)(counter.ResetTime - DateTime.UtcNow).TotalSeconds).ToString();

            return false;
        }

        // Increment counter
        counter.RequestCount++;
        _cache.Set(cacheKey, counter, counter.ResetTime);

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = rateLimit.RequestsPerDay.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = (rateLimit.RequestsPerDay - counter.RequestCount).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)counter.ResetTime).ToUnixTimeSeconds().ToString();

        return true;
    }

    private RateLimitConfig GetRateLimitForPlan(string planName)
    {
        return planName.ToUpperInvariant() switch
        {
            "FREE" => new RateLimitConfig { RequestsPerDay = 1000 },
            "BASIC" => new RateLimitConfig { RequestsPerDay = 10000 },
            "PRO" => new RateLimitConfig { RequestsPerDay = 100000 },
            "ENTERPRISE" => new RateLimitConfig { RequestsPerDay = 1000000 }, // Effectively unlimited
            _ => GetDefaultRateLimit()
        };
    }

    private RateLimitConfig GetDefaultRateLimit()
    {
        return new RateLimitConfig { RequestsPerDay = 1000 };
    }

    private class RateLimitConfig
    {
        public int RequestsPerDay { get; set; }
    }

    private class RateLimitCounter
    {
        public int TenantId { get; set; }
        public DateTime Date { get; set; }
        public int RequestCount { get; set; }
        public DateTime ResetTime { get; set; }
    }
}

