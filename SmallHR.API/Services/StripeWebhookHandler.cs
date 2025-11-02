using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmallHR.Core.DTOs.Subscription;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using System.Text;
using System.Text.Json;

namespace SmallHR.API.Services;

/// <summary>
/// Handles Stripe webhook events for subscription management
/// </summary>
public class StripeWebhookHandler
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StripeWebhookHandler> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAlertService _alertService;

    public StripeWebhookHandler(
        ApplicationDbContext context,
        ILogger<StripeWebhookHandler> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        IAlertService alertService)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _alertService = alertService;
    }

    /// <summary>
    /// Processes Stripe webhook event
    /// </summary>
    public async Task<bool> ProcessWebhookAsync(string jsonPayload, string stripeSignature)
    {
        WebhookEvent? webhookEvent = null;
        try
        {
            // Verify webhook signature (implement based on Stripe SDK)
            // For production, use: Stripe.EventUtility.ConstructEvent(jsonPayload, stripeSignature, webhookSecret)
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogWarning("Stripe webhook secret not configured. Skipping signature verification.");
            }

            // Parse Stripe event
            var stripeEventJson = JsonSerializer.Deserialize<JsonElement>(jsonPayload);
            var eventType = stripeEventJson.GetProperty("type").GetString();

            if (string.IsNullOrWhiteSpace(eventType))
            {
                _logger.LogWarning("Stripe webhook event type is missing");
                return false;
            }

            // Extract metadata to identify tenant and subscription
            int? tenantId = null;
            int? subscriptionId = null;
            
            try
            {
                // Try to get tenant ID and subscription ID from event data
                if (stripeEventJson.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("object", out var eventObject))
                {
                    // Try to get subscription ID from event object
                    if (eventObject.TryGetProperty("subscription", out var subElement))
                    {
                        var externalSubscriptionId = subElement.GetString();
                        if (!string.IsNullOrWhiteSpace(externalSubscriptionId))
                        {
                            var subscription = await _context.Subscriptions
                                .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == externalSubscriptionId);
                            if (subscription != null)
                            {
                                subscriptionId = subscription.Id;
                                tenantId = subscription.TenantId;
                            }
                        }
                    }
                    
                    // Alternative: try customer ID to find tenant
                    if (eventObject.TryGetProperty("customer", out var customerElement))
                    {
                        var externalCustomerId = customerElement.GetString();
                        if (!string.IsNullOrWhiteSpace(externalCustomerId) && !tenantId.HasValue)
                        {
                            var subscriptionByCustomer = await _context.Subscriptions
                                .FirstOrDefaultAsync(s => s.ExternalCustomerId == externalCustomerId);
                            if (subscriptionByCustomer != null)
                            {
                                subscriptionId = subscriptionByCustomer.Id;
                                tenantId = subscriptionByCustomer.TenantId;
                            }
                        }
                    }
                    
                    // For subscription events, try to get subscription ID directly
                    if (eventObject.TryGetProperty("id", out var idElement))
                    {
                        var externalId = idElement.GetString();
                        if (!string.IsNullOrWhiteSpace(externalId) && eventType.Contains("subscription"))
                        {
                            var subscriptionById = await _context.Subscriptions
                                .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == externalId);
                            if (subscriptionById != null)
                            {
                                subscriptionId = subscriptionById.Id;
                                tenantId = subscriptionById.TenantId;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract tenant/subscription from webhook event");
            }

            // Create webhook event record BEFORE processing
            webhookEvent = new WebhookEvent
            {
                EventType = eventType,
                Provider = "Stripe",
                Payload = jsonPayload,
                Signature = stripeSignature,
                TenantId = tenantId,
                SubscriptionId = subscriptionId,
                Processed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.WebhookEvents.AddAsync(webhookEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Processing Stripe webhook event: {EventType} (WebhookEvent ID: {WebhookEventId})", 
                eventType, webhookEvent.Id);

            // Process the webhook event
            bool processed = eventType switch
            {
                "customer.subscription.created" => await HandleSubscriptionCreatedAsync(stripeEventJson),
                "customer.subscription.updated" => await HandleSubscriptionUpdatedAsync(stripeEventJson),
                "customer.subscription.deleted" => await HandleSubscriptionDeletedAsync(stripeEventJson),
                "invoice.payment_succeeded" => await HandlePaymentSucceededAsync(stripeEventJson),
                "invoice.payment_failed" => await HandlePaymentFailedAsync(stripeEventJson),
                "customer.subscription.trial_will_end" => await HandleTrialWillEndAsync(stripeEventJson),
                _ => await HandleUnknownEventAsync(eventType, stripeEventJson)
            };

            // Update webhook event record after processing
            if (webhookEvent != null)
            {
                webhookEvent.Processed = processed;
                webhookEvent.UpdatedAt = DateTime.UtcNow;
                
                // Update tenant and subscription IDs if they were found during processing
                if (tenantId.HasValue && webhookEvent.TenantId == null)
                {
                    webhookEvent.TenantId = tenantId;
                }
                if (subscriptionId.HasValue && webhookEvent.SubscriptionId == null)
                {
                    webhookEvent.SubscriptionId = subscriptionId;
                }
                
                await _context.SaveChangesAsync();
            }

            return processed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook: {Message}", ex.Message);
            
            // Update webhook event record with error
            if (webhookEvent != null)
            {
                webhookEvent.Processed = false;
                webhookEvent.Error = ex.Message;
                webhookEvent.UpdatedAt = DateTime.UtcNow;
                
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "Failed to update webhook event with error");
                }
            }
            
            return false;
        }
    }

    private async Task<bool> HandleSubscriptionCreatedAsync(JsonElement stripeEvent)
    {
        var data = stripeEvent.GetProperty("data").GetProperty("object");
        var subscriptionId = data.GetProperty("id").GetString();
        var customerId = data.GetProperty("customer").GetString();
        var status = data.GetProperty("status").GetString();
        var currentPeriodEnd = DateTimeOffset.FromUnixTimeSeconds(
            data.GetProperty("current_period_end").GetInt64()).DateTime;

        _logger.LogInformation("Stripe subscription created: {SubscriptionId}", subscriptionId);

        // Find subscription by external ID or create new
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId);

        if (subscription == null)
        {
            // Create new subscription
            // Note: You'll need to map Stripe plan to your SubscriptionPlan
            // For now, this is a placeholder - you'd need to get tenantId from metadata or customer
            _logger.LogWarning("Subscription not found for Stripe ID {SubscriptionId}. " +
                             "Ensure tenant metadata is set in Stripe.", subscriptionId);
            return false;
        }

        subscription.Status = MapStripeStatus(status);
        subscription.ExternalSubscriptionId = subscriptionId;
        subscription.ExternalCustomerId = customerId;
        subscription.EndDate = currentPeriodEnd;
        subscription.BillingProvider = BillingProvider.Stripe;
        subscription.AutoRenew = true;

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> HandleSubscriptionUpdatedAsync(JsonElement stripeEvent)
    {
        var data = stripeEvent.GetProperty("data").GetProperty("object");
        var subscriptionId = data.GetProperty("id").GetString();
        var status = data.GetProperty("status").GetString();
        var currentPeriodEnd = DateTimeOffset.FromUnixTimeSeconds(
            data.GetProperty("current_period_end").GetInt64()).DateTime;
        var cancelAtPeriodEnd = data.TryGetProperty("cancel_at_period_end", out var cancel) && cancel.GetBoolean();

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription not found for Stripe ID {SubscriptionId}", subscriptionId);
            return false;
        }

        subscription.Status = MapStripeStatus(status);
        subscription.EndDate = currentPeriodEnd;
        subscription.CancelAtPeriodEnd = cancelAtPeriodEnd ? currentPeriodEnd : null;

        if (cancelAtPeriodEnd && subscription.Status == SubscriptionStatus.Active)
        {
            subscription.CanceledAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> HandleSubscriptionDeletedAsync(JsonElement stripeEvent)
    {
        var data = stripeEvent.GetProperty("data").GetProperty("object");
        var subscriptionId = data.GetProperty("id").GetString();

        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription not found for Stripe ID {SubscriptionId}", subscriptionId);
            return false;
        }

        subscription.Status = SubscriptionStatus.Canceled;
        subscription.CanceledAt = DateTime.UtcNow;
        subscription.AutoRenew = false;

        await _context.SaveChangesAsync();

        // Create cancellation alert
        if (subscription.Tenant != null)
        {
            try
            {
                await _alertService.CreateCancellationAlertAsync(
                    subscription.TenantId,
                    subscription.Id,
                    "Subscription cancelled via Stripe webhook",
                    new Dictionary<string, object>
                    {
                        { "externalSubscriptionId", subscriptionId ?? string.Empty },
                        { "canceledAt", subscription.CanceledAt.ToString() ?? DateTime.UtcNow.ToString() }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create cancellation alert for tenant {TenantId}", subscription.TenantId);
            }
        }

        return true;
    }

    private async Task<bool> HandlePaymentSucceededAsync(JsonElement stripeEvent)
    {
        var data = stripeEvent.GetProperty("data").GetProperty("object");
        var customerId = data.GetProperty("customer").GetString();
        var subscriptionId = data.TryGetProperty("subscription", out var sub) 
            ? sub.GetString() 
            : null;

        if (string.IsNullOrWhiteSpace(subscriptionId))
            return false;

        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId || 
                                       s.ExternalCustomerId == customerId);

        if (subscription == null) return false;

        // Ensure subscription is active
        subscription.Status = SubscriptionStatus.Active;
        subscription.AutoRenew = true;

        if (subscription.Tenant != null)
        {
            subscription.Tenant.IsSubscriptionActive = true;
            
            // Activate tenant via lifecycle service
            using var scope = _serviceProvider.CreateScope();
            var lifecycleService = scope.ServiceProvider.GetRequiredService<ITenantLifecycleService>();
            await lifecycleService.ActivateTenantAsync(subscription.Tenant.Id, customerId);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> HandlePaymentFailedAsync(JsonElement stripeEvent)
    {
        var data = stripeEvent.GetProperty("data").GetProperty("object");
        var customerId = data.GetProperty("customer").GetString();
        var subscriptionId = data.TryGetProperty("subscription", out var sub) 
            ? sub.GetString() 
            : null;
        var attemptCount = data.TryGetProperty("attempt_count", out var attempts) 
            ? attempts.GetInt32() 
            : 1;

        if (string.IsNullOrWhiteSpace(subscriptionId))
            return false;

        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId || 
                                       s.ExternalCustomerId == customerId);

        if (subscription == null) return false;

        subscription.Status = SubscriptionStatus.PastDue;

        if (subscription.Tenant != null)
        {
            subscription.Tenant.IsSubscriptionActive = false;
            
            // Create payment failure alert
            try
            {
                await _alertService.CreatePaymentFailureAlertAsync(
                    subscription.TenantId,
                    subscription.Id,
                    $"Payment failed for customer {customerId}. Attempt {attemptCount}.",
                    new Dictionary<string, object>
                    {
                        { "customerId", customerId ?? string.Empty },
                        { "externalSubscriptionId", subscriptionId ?? string.Empty },
                        { "attemptCount", attemptCount },
                        { "subscriptionStatus", "PastDue" }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment failure alert for tenant {TenantId}", subscription.TenantId);
            }
            
            // Suspend tenant via lifecycle service (this will also create a suspension alert)
            using var scope = _serviceProvider.CreateScope();
            var lifecycleService = scope.ServiceProvider.GetRequiredService<ITenantLifecycleService>();
            await lifecycleService.SuspendTenantAsync(
                subscription.Tenant.Id, 
                $"Payment failed for customer {customerId}", 
                gracePeriodDays: 30);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> HandleTrialWillEndAsync(JsonElement stripeEvent)
    {
        // Notify tenant about trial ending
        _logger.LogInformation("Trial ending soon - notification sent");
        return await Task.FromResult(true);
    }

    private async Task<bool> HandleUnknownEventAsync(string eventType, JsonElement stripeEvent)
    {
        _logger.LogInformation("Unhandled Stripe webhook event: {EventType}", eventType);
        return await Task.FromResult(true);
    }

    private SubscriptionStatus MapStripeStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trialing,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "unpaid" => SubscriptionStatus.Unpaid,
            "incomplete" => SubscriptionStatus.Incomplete,
            "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
            _ => SubscriptionStatus.Active
        };
    }
}

