using SmallHR.Core.DTOs.Subscription;
using SmallHR.Core.Entities;

namespace SmallHR.Core.Interfaces;

public interface ISubscriptionService
{
    // Subscription Management
    Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request);
    Task<SubscriptionDto?> GetSubscriptionByTenantIdAsync(int tenantId);
    Task<SubscriptionDto?> GetSubscriptionByIdAsync(int subscriptionId);
    Task<SubscriptionDto> UpdateSubscriptionAsync(int subscriptionId, UpdateSubscriptionRequest request);
    Task<bool> CancelSubscriptionAsync(int subscriptionId, string? reason = null);
    Task<bool> ReactivateSubscriptionAsync(int subscriptionId);
    
    // Subscription Plans
    Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync();
    Task<SubscriptionPlanDto?> GetPlanByIdAsync(int planId);
    Task<SubscriptionPlanDto?> GetPlanByNameAsync(string planName);
    
    // Feature Checking
    Task<bool> HasFeatureAsync(int tenantId, string featureKey);
    Task<string?> GetFeatureValueAsync(int tenantId, string featureKey);
    Task<Dictionary<string, string>> GetTenantFeaturesAsync(int tenantId);
    
    // Limits
    Task<bool> CheckEmployeeLimitAsync(int tenantId);
    Task<bool> CheckUserLimitAsync(int tenantId);
    Task<int> GetMaxEmployeesAsync(int tenantId);
    Task<int?> GetMaxUsersAsync(int tenantId);
    
    // Status
    Task<bool> IsSubscriptionActiveAsync(int tenantId);
    Task<bool> IsInTrialAsync(int tenantId);
    
    // Billing Provider Integration
    Task<bool> SyncSubscriptionFromProviderAsync(string externalSubscriptionId, BillingProvider provider);
    Task<SubscriptionDto> CreateOrUpdateSubscriptionFromWebhookAsync(WebhookEventDto webhookEvent);
}

