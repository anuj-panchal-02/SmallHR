using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallHR.Core.DTOs.Subscription;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<SubscriptionService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request)
    {
        var tenant = await _context.Tenants.FindAsync(request.TenantId);
        if (tenant == null)
            throw new ArgumentException($"Tenant with ID {request.TenantId} not found");

        var plan = await _context.SubscriptionPlans.FindAsync(request.SubscriptionPlanId);
        if (plan == null)
            throw new ArgumentException($"Subscription plan with ID {request.SubscriptionPlanId} not found");

        // Check if tenant already has an active subscription
        var existingSubscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == request.TenantId && 
                                      s.Status != SubscriptionStatus.Canceled &&
                                      s.Status != SubscriptionStatus.Expired);

        if (existingSubscription != null)
            throw new InvalidOperationException($"Tenant already has an active subscription");

        // Determine price based on billing period
        var price = request.BillingPeriod switch
        {
            BillingPeriod.Monthly => plan.MonthlyPrice,
            BillingPeriod.Quarterly => plan.QuarterlyPrice ?? plan.MonthlyPrice * 3,
            BillingPeriod.Yearly => plan.YearlyPrice ?? plan.MonthlyPrice * 12,
            _ => plan.MonthlyPrice
        };

        var subscription = new Subscription
        {
            TenantId = request.TenantId,
            SubscriptionPlanId = request.SubscriptionPlanId,
            Status = request.StartTrial && plan.TrialDays.HasValue 
                ? SubscriptionStatus.Trialing 
                : SubscriptionStatus.Active,
            BillingPeriod = request.BillingPeriod,
            StartDate = DateTime.UtcNow,
            EndDate = request.BillingPeriod switch
            {
                BillingPeriod.Monthly => DateTime.UtcNow.AddMonths(1),
                BillingPeriod.Quarterly => DateTime.UtcNow.AddMonths(3),
                BillingPeriod.Yearly => DateTime.UtcNow.AddYears(1),
                _ => null
            },
            TrialEndDate = request.StartTrial && plan.TrialDays.HasValue
                ? DateTime.UtcNow.AddDays(plan.TrialDays.Value)
                : null,
            Price = price,
            Currency = plan.Currency,
            AutoRenew = true,
            BillingProvider = BillingProvider.None
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return await GetSubscriptionByIdAsync(subscription.Id) ?? throw new InvalidOperationException("Failed to create subscription");
    }

    public async Task<SubscriptionDto?> GetSubscriptionByTenantIdAsync(int tenantId)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .Include(s => s.Plan)
                .ThenInclude(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        return subscription == null ? null : MapToDto(subscription);
    }

    public async Task<SubscriptionDto?> GetSubscriptionByIdAsync(int subscriptionId)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .Include(s => s.Plan)
                .ThenInclude(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);

        return subscription == null ? null : MapToDto(subscription);
    }

    public async Task<SubscriptionDto> UpdateSubscriptionAsync(int subscriptionId, UpdateSubscriptionRequest request)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);

        if (subscription == null)
            throw new ArgumentException($"Subscription with ID {subscriptionId} not found");

        if (request.SubscriptionPlanId.HasValue)
        {
            var newPlan = await _context.SubscriptionPlans.FindAsync(request.SubscriptionPlanId.Value);
            if (newPlan == null)
                throw new ArgumentException($"Subscription plan with ID {request.SubscriptionPlanId.Value} not found");

            subscription.SubscriptionPlanId = newPlan.Id;
            var billingPeriodToUse = request.BillingPeriod ?? subscription.BillingPeriod;
            subscription.Price = billingPeriodToUse switch
            {
                BillingPeriod.Monthly => newPlan.MonthlyPrice,
                BillingPeriod.Quarterly => newPlan.QuarterlyPrice ?? newPlan.MonthlyPrice * 3,
                BillingPeriod.Yearly => newPlan.YearlyPrice ?? newPlan.MonthlyPrice * 12,
                _ => newPlan.MonthlyPrice
            };
        }

        if (request.BillingPeriod.HasValue)
        {
            subscription.BillingPeriod = request.BillingPeriod.Value;
            var plan = await _context.SubscriptionPlans.FindAsync(subscription.SubscriptionPlanId);
            if (plan != null)
            {
                subscription.Price = request.BillingPeriod.Value switch
                {
                    BillingPeriod.Monthly => plan.MonthlyPrice,
                    BillingPeriod.Quarterly => plan.QuarterlyPrice ?? plan.MonthlyPrice * 3,
                    BillingPeriod.Yearly => plan.YearlyPrice ?? plan.MonthlyPrice * 12,
                    _ => plan.MonthlyPrice
                };
            }
        }

        if (request.AutoRenew.HasValue)
            subscription.AutoRenew = request.AutoRenew.Value;

        if (request.CancelAtPeriodEnd.HasValue)
            subscription.CancelAtPeriodEnd = request.CancelAtPeriodEnd.Value;

        if (!string.IsNullOrWhiteSpace(request.CancellationReason))
            subscription.CancellationReason = request.CancellationReason;

        await _context.SaveChangesAsync();

        return await GetSubscriptionByIdAsync(subscriptionId) ?? throw new InvalidOperationException("Failed to update subscription");
    }

    public async Task<bool> CancelSubscriptionAsync(int subscriptionId, string? reason = null)
    {
        var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
            return false;

        subscription.Status = SubscriptionStatus.Canceled;
        subscription.CanceledAt = DateTime.UtcNow;
        subscription.CancellationReason = reason;
        subscription.AutoRenew = false;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReactivateSubscriptionAsync(int subscriptionId)
    {
        var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
            return false;

        if (subscription.EndDate.HasValue && subscription.EndDate < DateTime.UtcNow)
        {
            // Subscription has expired, renew it
            var plan = await _context.SubscriptionPlans.FindAsync(subscription.SubscriptionPlanId);
            if (plan == null) return false;

            subscription.EndDate = subscription.BillingPeriod switch
            {
                BillingPeriod.Monthly => DateTime.UtcNow.AddMonths(1),
                BillingPeriod.Quarterly => DateTime.UtcNow.AddMonths(3),
                BillingPeriod.Yearly => DateTime.UtcNow.AddYears(1),
                _ => null
            };
        }

        subscription.Status = SubscriptionStatus.Active;
        subscription.CanceledAt = null;
        subscription.AutoRenew = true;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync()
    {
        var plans = await _context.SubscriptionPlans
            .Where(sp => sp.IsActive && sp.IsVisible)
            .Include(sp => sp.PlanFeatures)
                .ThenInclude(pf => pf.Feature)
            .OrderBy(sp => sp.DisplayOrder)
            .ToListAsync();

        return plans.Select(MapPlanToDto).ToList();
    }

    public async Task<SubscriptionPlanDto?> GetPlanByIdAsync(int planId)
    {
        var plan = await _context.SubscriptionPlans
            .Include(sp => sp.PlanFeatures)
                .ThenInclude(pf => pf.Feature)
            .FirstOrDefaultAsync(sp => sp.Id == planId);

        return plan == null ? null : MapPlanToDto(plan);
    }

    public async Task<SubscriptionPlanDto?> GetPlanByNameAsync(string planName)
    {
        var plan = await _context.SubscriptionPlans
            .Include(sp => sp.PlanFeatures)
                .ThenInclude(pf => pf.Feature)
            .FirstOrDefaultAsync(sp => sp.Name == planName);

        return plan == null ? null : MapPlanToDto(plan);
    }

    public async Task<bool> HasFeatureAsync(int tenantId, string featureKey)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Plan)
                .ThenInclude(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && 
                                      (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing));

        if (subscription == null) return false;

        return subscription.Plan.PlanFeatures
            .Any(pf => pf.Feature.Key == featureKey && 
                      (pf.Value == "true" || !string.IsNullOrWhiteSpace(pf.Value)));
    }

    public async Task<string?> GetFeatureValueAsync(int tenantId, string featureKey)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Plan)
                .ThenInclude(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && 
                                      (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing));

        if (subscription == null) return null;

        var planFeature = subscription.Plan.PlanFeatures
            .FirstOrDefault(pf => pf.Feature.Key == featureKey);

        return planFeature?.Value;
    }

    public async Task<Dictionary<string, string>> GetTenantFeaturesAsync(int tenantId)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Plan)
                .ThenInclude(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && 
                                      (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing));

        if (subscription == null) return new Dictionary<string, string>();

        return subscription.Plan.PlanFeatures
            .Where(pf => !string.IsNullOrWhiteSpace(pf.Value))
            .ToDictionary(pf => pf.Feature.Key, pf => pf.Value!);
    }

    public async Task<bool> CheckEmployeeLimitAsync(int tenantId)
    {
        var subscription = await GetSubscriptionByTenantIdAsync(tenantId);
        if (subscription == null) return false;

        var currentCount = await _context.Employees.CountAsync(e => e.TenantId == _context.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.Name)
            .FirstOrDefault());

        // Get max employees from plan
        var plan = await GetPlanByIdAsync(subscription.SubscriptionPlanId);
        if (plan == null) return false;

        return currentCount < plan.MaxEmployees;
    }

    public async Task<bool> CheckUserLimitAsync(int tenantId)
    {
        var subscription = await GetSubscriptionByTenantIdAsync(tenantId);
        if (subscription == null) return true; // No subscription means no limit

        var plan = await GetPlanByIdAsync(subscription.SubscriptionPlanId);
        if (plan == null || !plan.MaxUsers.HasValue) return true; // No limit

        // Count users for tenant (would need tenant identifier on User entity)
        // For now, return true as placeholder
        return true;
    }

    public async Task<int> GetMaxEmployeesAsync(int tenantId)
    {
        var subscription = await GetSubscriptionByTenantIdAsync(tenantId);
        if (subscription == null) return 0;

        var plan = await GetPlanByIdAsync(subscription.SubscriptionPlanId);
        return plan?.MaxEmployees ?? 0;
    }

    public async Task<int?> GetMaxUsersAsync(int tenantId)
    {
        var subscription = await GetSubscriptionByTenantIdAsync(tenantId);
        if (subscription == null) return null;

        var plan = await GetPlanByIdAsync(subscription.SubscriptionPlanId);
        return plan?.MaxUsers;
    }

    public async Task<bool> IsSubscriptionActiveAsync(int tenantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (subscription == null) return false;

        return subscription.Status == SubscriptionStatus.Active ||
               subscription.Status == SubscriptionStatus.Trialing;
    }

    public async Task<bool> IsInTrialAsync(int tenantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (subscription == null) return false;

        return subscription.Status == SubscriptionStatus.Trialing &&
               subscription.TrialEndDate.HasValue &&
               subscription.TrialEndDate > DateTime.UtcNow;
    }

    public async Task<bool> SyncSubscriptionFromProviderAsync(string externalSubscriptionId, BillingProvider provider)
    {
        // Placeholder for provider-specific sync logic
        // This would call Stripe/Paddle API to get latest subscription status
        _logger.LogInformation("Syncing subscription {SubscriptionId} from provider {Provider}", 
            externalSubscriptionId, provider);
        return true;
    }

    public async Task<SubscriptionDto> CreateOrUpdateSubscriptionFromWebhookAsync(WebhookEventDto webhookEvent)
    {
        // This will be implemented by Stripe/Paddle webhook handlers
        throw new NotImplementedException("Use specific webhook handlers (StripeWebhookHandler, PaddleWebhookHandler)");
    }

    private SubscriptionDto MapToDto(Subscription subscription)
    {
        return new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            TenantName = subscription.Tenant.Name,
            SubscriptionPlanId = subscription.SubscriptionPlanId,
            PlanName = subscription.Plan.Name,
            Status = subscription.Status.ToString(),
            BillingPeriod = subscription.BillingPeriod.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndDate = subscription.TrialEndDate,
            CanceledAt = subscription.CanceledAt,
            Price = subscription.Price,
            Currency = subscription.Currency,
            AutoRenew = subscription.AutoRenew,
            ExternalSubscriptionId = subscription.ExternalSubscriptionId,
            ExternalCustomerId = subscription.ExternalCustomerId,
            BillingProvider = subscription.BillingProvider.ToString(),
            Features = subscription.Plan.PlanFeatures.Select(pf => new FeatureDto
            {
                Id = pf.Feature.Id,
                Key = pf.Feature.Key,
                Name = pf.Feature.Name,
                Description = pf.Feature.Description,
                Category = pf.Feature.Category,
                Type = pf.Feature.Type.ToString(),
                Value = pf.Value
            }).ToList()
        };
    }

    private SubscriptionPlanDto MapPlanToDto(SubscriptionPlan plan)
    {
        return new SubscriptionPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            MonthlyPrice = plan.MonthlyPrice,
            YearlyPrice = plan.YearlyPrice,
            QuarterlyPrice = plan.QuarterlyPrice,
            Currency = plan.Currency,
            MaxEmployees = plan.MaxEmployees,
            MaxDepartments = plan.MaxDepartments,
            MaxUsers = plan.MaxUsers,
            MaxStorageBytes = plan.MaxStorageBytes,
            TrialDays = plan.TrialDays,
            IsActive = plan.IsActive,
            IsVisible = plan.IsVisible,
            PopularBadge = plan.PopularBadge,
            Icon = plan.Icon,
            Features = plan.PlanFeatures.Select(pf => new FeatureDto
            {
                Id = pf.Feature.Id,
                Key = pf.Feature.Key,
                Name = pf.Feature.Name,
                Description = pf.Feature.Description,
                Category = pf.Feature.Category,
                Type = pf.Feature.Type.ToString(),
                Value = pf.Value
            }).ToList()
        };
    }
}

