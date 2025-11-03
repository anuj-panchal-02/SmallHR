using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Base;
using SmallHR.API.Services;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using System.Text;

namespace SmallHR.API.Controllers;

/// <summary>
/// Webhook controller for billing providers (Stripe, Paddle)
/// Note: Webhook events are automatically saved to the database by StripeWebhookHandler
/// </summary>
[ApiController]
[Route("api/webhooks/[controller]")]
public class BillingWebhooksController : BaseApiController
{
    private readonly StripeWebhookHandler _stripeWebhookHandler;
    private readonly ApplicationDbContext _context;

    public BillingWebhooksController(
        StripeWebhookHandler stripeWebhookHandler,
        ApplicationDbContext context,
        ILogger<BillingWebhooksController> logger) : base(logger)
    {
        _stripeWebhookHandler = stripeWebhookHandler;
        _context = context;
    }

    /// <summary>
    /// Stripe webhook endpoint
    /// Configure in Stripe Dashboard: https://dashboard.stripe.com/webhooks
    /// 
    /// Note: In development, this endpoint works without Stripe configuration.
    /// For production, configure Stripe webhook secret in appsettings.json
    /// 
    /// Webhook events are automatically saved to the WebhookEvents table by StripeWebhookHandler
    /// before processing. The Processed flag is updated after successful processing.
    /// </summary>
    [HttpPost("stripe")]
    public async Task<ActionResult<object>> StripeWebhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonPayload = await reader.ReadToEndAsync();

            var signature = Request.Headers["Stripe-Signature"].ToString();

            // StripeWebhookHandler.ProcessWebhookAsync() automatically:
            // 1. Saves webhook event to database BEFORE processing
            // 2. Processes the webhook event
            // 3. Updates Processed flag after successful processing
            // 4. Stores error messages if processing fails
            var result = await _stripeWebhookHandler.ProcessWebhookAsync(jsonPayload, signature);

            if (result)
            {
                Logger.LogInformation("Stripe webhook processed successfully. Event saved to database.");
                return Ok(new { received = true, message = "Webhook processed successfully" });
            }

            Logger.LogWarning("Stripe webhook processing failed. Event saved to database but not processed.");
            return CreateBadRequestResponse("Failed to process webhook");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing Stripe webhook: {Message}", ex.Message);
            
            // In development, return 200 to avoid webhook retries from Stripe
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                Logger.LogWarning("Development mode: Returning success despite error to prevent webhook retries");
                return Ok(new { received = true, warning = "Development mode: Webhook not fully processed" });
            }
            
            return CreateErrorResponse("Internal server error processing webhook", ex);
        }
    }

    /// <summary>
    /// Paddle webhook endpoint (placeholder)
    /// Configure in Paddle Dashboard
    /// 
    /// TODO: Implement Paddle webhook handler similar to StripeWebhookHandler
    /// - Save webhook events to database before processing
    /// - Process Paddle-specific events (subscription.created, subscription.updated, etc.)
    /// - Update Processed flag after successful processing
    /// - Store error messages if processing fails
    /// </summary>
    [HttpPost("paddle")]
    public async Task<ActionResult<object>> PaddleWebhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonPayload = await reader.ReadToEndAsync();

            var signature = Request.Headers["Paddle-Signature"].ToString();

            // TODO: Parse Paddle event type
            // TODO: Save webhook event to database (similar to StripeWebhookHandler)
            // For now, save a basic webhook event record
            try
            {
                var webhookEvent = new WebhookEvent
                {
                    EventType = "paddle.webhook.received",
                    Provider = "Paddle",
                    Payload = jsonPayload,
                    Signature = signature,
                    Processed = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.WebhookEvents.AddAsync(webhookEvent);
                await _context.SaveChangesAsync();

                Logger.LogInformation("Paddle webhook received and saved to database (ID: {WebhookEventId}). Handler not yet implemented.", 
                    webhookEvent.Id);
            }
            catch (Exception saveEx)
            {
                Logger.LogError(saveEx, "Failed to save Paddle webhook event to database");
            }

            // Paddle webhook handler implementation would go here
            Logger.LogInformation("Paddle webhook received - handler not yet implemented");
            return Ok(new { received = true, message = "Paddle webhook handler not implemented" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing Paddle webhook: {Message}", ex.Message);
            
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                return Ok(new { received = true, warning = "Development mode: Paddle webhook not processed" });
            }
            
            return CreateErrorResponse("Internal server error processing Paddle webhook", ex);
        }
    }
}

