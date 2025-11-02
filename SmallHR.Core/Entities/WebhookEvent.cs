using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.Entities;

/// <summary>
/// Webhook event entity for tracking billing provider webhook events (Stripe, Paddle, etc.)
/// </summary>
public class WebhookEvent : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty; // e.g., "subscription.created", "payment.failed"
    
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty; // Stripe, Paddle, PayPal
    
    [Required]
    public string Payload { get; set; } = string.Empty; // JSON payload
    
    public bool Processed { get; set; } = false;
    
    public string? Error { get; set; } // Error message if processing failed
    
    [MaxLength(500)]
    public string? Signature { get; set; } // Webhook signature for verification
    
    public int? TenantId { get; set; } // Associated tenant if applicable
    
    public int? SubscriptionId { get; set; } // Associated subscription if applicable
    
    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual Subscription? Subscription { get; set; }
}

