# Subscription & Feature Management

## Overview

SmallHR implements a comprehensive subscription and billing system with:
- **Separate Subscription entity** linked to Tenant
- **Feature-based access control** via Feature flags
- **Multiple subscription plans** (Free, Basic, Pro, Enterprise)
- **Billing provider integration** (Stripe, Paddle)
- **Webhook handling** for real-time subscription updates

---

## Architecture

### Entity Relationships

```
Tenant (1) ───── (1) Subscription
                      │
                      │
                      ▼
                 SubscriptionPlan (M)
                      │
                      │
                      ▼
            SubscriptionPlanFeature (Junction Table)
                      │
                      │
                      ▼
                  Feature (M)
```

### Key Entities

1. **Subscription** - Tracks tenant's active subscription
   - Links to Tenant (1:1)
   - Links to SubscriptionPlan
   - Tracks billing provider (Stripe, Paddle)
   - Stores external subscription/customer IDs
   - Manages subscription status and lifecycle

2. **SubscriptionPlan** - Defines available plans
   - Pricing (Monthly, Quarterly, Yearly)
   - Limits (Max Employees, Users, Storage)
   - External billing provider IDs (Stripe Price IDs, Paddle Plan IDs)
   - Features included in plan

3. **Feature** - Feature flags for access control
   - Unique feature keys (e.g., "advanced_analytics", "custom_integrations")
   - Feature types (Boolean, Limit, Enum)
   - Categorized by category (analytics, integrations, security)

4. **SubscriptionPlanFeature** - Junction table
   - Links plans to features
   - Stores feature values (for limit-based features)

---

## Subscription Status

```csharp
public enum SubscriptionStatus
{
    Active = 1,        // Active and paid
    Trialing = 2,      // In trial period
    PastDue = 3,       // Payment failed, needs attention
    Canceled = 4,      // Canceled by user
    Unpaid = 5,        // Unpaid, access suspended
    Expired = 6,       // Subscription expired
    Incomplete = 7,    // Incomplete setup
    IncompleteExpired = 8  // Incomplete setup expired
}
```

---

## Feature Management

### Feature Types

1. **Boolean Features** - Simple enabled/disabled
   - Example: `advanced_analytics`, `custom_integrations`
   - Value: `"true"` or `"false"`

2. **Limit Features** - Numeric limits
   - Example: `max_employees`, `max_storage_bytes`
   - Value: `"50"`, `"1000000"` (numeric string)

3. **Enum Features** - Multiple value options
   - Example: `support_level` (Basic, Priority, 24/7)
   - Value: `"Priority"`

### Checking Features

```csharp
// In your service/controller
var hasFeature = await _subscriptionService.HasFeatureAsync(tenantId, "advanced_analytics");
var featureValue = await _subscriptionService.GetFeatureValueAsync(tenantId, "max_employees");
var allFeatures = await _subscriptionService.GetTenantFeaturesAsync(tenantId);
```

---

## API Endpoints

### Subscription Management

#### Get Current Subscription
```http
GET /api/subscriptions/current
Authorization: Bearer {token}
```

#### Get Subscription by ID (Admin)
```http
GET /api/subscriptions/{id}
Authorization: Bearer {token}
```

#### Get Subscription by Tenant ID (Admin)
```http
GET /api/subscriptions/tenant/{tenantId}
Authorization: Bearer {token}
```

#### Create Subscription (SuperAdmin)
```http
POST /api/subscriptions
Content-Type: application/json
Authorization: Bearer {token}

{
  "tenantId": 1,
  "subscriptionPlanId": 2,
  "billingPeriod": "Monthly",
  "startTrial": true
}
```

#### Update Subscription (Admin)
```http
PUT /api/subscriptions/{id}
Content-Type: application/json
Authorization: Bearer {token}

{
  "subscriptionPlanId": 3,
  "billingPeriod": "Yearly",
  "autoRenew": true,
  "cancelAtPeriodEnd": null
}
```

#### Cancel Subscription
```http
POST /api/subscriptions/{id}/cancel?reason=Too expensive
Authorization: Bearer {token}
```

#### Reactivate Subscription
```http
POST /api/subscriptions/{id}/reactivate
Authorization: Bearer {token}
```

### Subscription Plans

#### Get Available Plans
```http
GET /api/subscriptions/plans
```

Response:
```json
[
  {
    "id": 1,
    "name": "Free",
    "description": "Basic HR features",
    "monthlyPrice": 0,
    "yearlyPrice": 0,
    "maxEmployees": 10,
    "trialDays": null,
    "features": [
      {
        "key": "basic_hr",
        "name": "Basic HR Features",
        "value": "true"
      }
    ]
  },
  {
    "id": 2,
    "name": "Basic",
    "monthlyPrice": 99,
    "yearlyPrice": 990,
    "maxEmployees": 50,
    "trialDays": 14,
    "popularBadge": "Most Popular"
  }
]
```

#### Get Plan by ID
```http
GET /api/subscriptions/plans/{id}
```

### Feature Checking

#### Check Feature
```http
GET /api/subscriptions/features/{featureKey}
Authorization: Bearer {token}
```

#### Get All Features
```http
GET /api/subscriptions/features
Authorization: Bearer {token}
```

---

## Billing Provider Integration

### Stripe Integration

#### Setup

1. **Install Stripe .NET SDK** (optional, for signature verification):
```bash
dotnet add package Stripe.net
```

2. **Configure Stripe Settings** in `appsettings.json`:
```json
{
  "Stripe": {
    "ApiKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "PublishableKey": "pk_test_..."
  }
}
```

3. **Configure Webhook in Stripe Dashboard**:
   - Go to: https://dashboard.stripe.com/webhooks
   - Add endpoint: `https://yourdomain.com/api/webhooks/billing/stripe`
   - Select events:
     - `customer.subscription.created`
     - `customer.subscription.updated`
     - `customer.subscription.deleted`
     - `invoice.payment_succeeded`
     - `invoice.payment_failed`
     - `customer.subscription.trial_will_end`

#### Webhook Endpoint

```http
POST /api/webhooks/billing/stripe
Stripe-Signature: {signature}
Content-Type: application/json

{
  "type": "customer.subscription.updated",
  "data": {
    "object": {
      "id": "sub_...",
      "customer": "cus_...",
      "status": "active",
      ...
    }
  }
}
```

### Paddle Integration

Paddle webhook handler is a placeholder. To implement:

1. **Configure Paddle Settings** in `appsettings.json`:
```json
{
  "Paddle": {
    "VendorId": "your_vendor_id",
    "ApiKey": "your_api_key",
    "PublicKey": "your_public_key"
  }
}
```

2. **Create PaddleWebhookHandler** similar to `StripeWebhookHandler`
3. **Configure webhook in Paddle Dashboard**

---

## Service Usage Examples

### Checking Subscription Limits

```csharp
// In EmployeeService or similar
public class EmployeeService
{
    private readonly ISubscriptionService _subscriptionService;

    public async Task<Employee> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        var tenantId = GetTenantId(); // From context
        
        // Check if subscription is active
        var isActive = await _subscriptionService.IsSubscriptionActiveAsync(tenantId);
        if (!isActive)
        {
            throw new InvalidOperationException("Subscription is not active");
        }

        // Check employee limit
        var canAddEmployee = await _subscriptionService.CheckEmployeeLimitAsync(tenantId);
        if (!canAddEmployee)
        {
            var maxEmployees = await _subscriptionService.GetMaxEmployeesAsync(tenantId);
            throw new InvalidOperationException(
                $"Employee limit reached. Maximum: {maxEmployees}");
        }

        // Create employee...
    }
}
```

### Checking Feature Access

```csharp
// In your controller/service
public class AnalyticsController
{
    private readonly ISubscriptionService _subscriptionService;

    [HttpGet("advanced")]
    public async Task<IActionResult> GetAdvancedAnalytics()
    {
        var tenantId = GetTenantId();
        
        var hasFeature = await _subscriptionService.HasFeatureAsync(
            tenantId, "advanced_analytics");
        
        if (!hasFeature)
        {
            return Forbid("Advanced analytics not available in your plan");
        }

        // Return advanced analytics...
    }
}
```

---

## Database Migration

### Create Migration

```bash
cd SmallHR.Infrastructure
dotnet ef migrations add AddSubscriptionAndBillingEntities --startup-project ../SmallHR.API
```

### Apply Migration

```bash
dotnet ef database update --startup-project ../SmallHR.API
```

### Migration Includes

- `Subscriptions` table
- `SubscriptionPlans` table
- `Features` table
- `SubscriptionPlanFeatures` junction table
- Foreign key relationships
- Indexes for performance

---

## Seed Data

### Seeding Subscription Plans

Create a seed method to populate default plans:

```csharp
// Example seed data
var freePlan = new SubscriptionPlan
{
    Name = "Free",
    MonthlyPrice = 0,
    MaxEmployees = 10,
    IsActive = true,
    IsVisible = true,
    DisplayOrder = 1
};

var basicPlan = new SubscriptionPlan
{
    Name = "Basic",
    MonthlyPrice = 99,
    YearlyPrice = 990,
    MaxEmployees = 50,
    TrialDays = 14,
    IsActive = true,
    IsVisible = true,
    DisplayOrder = 2,
    PopularBadge = "Most Popular"
};

// Add features to plans
var advancedAnalyticsFeature = new Feature
{
    Key = "advanced_analytics",
    Name = "Advanced Analytics",
    Type = FeatureType.Boolean
};

basicPlan.PlanFeatures.Add(new SubscriptionPlanFeature
{
    Feature = advancedAnalyticsFeature,
    Value = "true"
});
```

---

## Configuration

### appsettings.json

```json
{
  "Stripe": {
    "ApiKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "PublishableKey": "pk_test_..."
  },
  "Paddle": {
    "VendorId": "your_vendor_id",
    "ApiKey": "your_api_key",
    "PublicKey": "your_public_key"
  }
}
```

### Environment Variables

For production, use environment variables:

```bash
export Stripe__ApiKey="sk_live_..."
export Stripe__WebhookSecret="whsec_..."
export Paddle__ApiKey="your_api_key"
```

---

## Testing

### Testing Webhooks Locally

Use **Stripe CLI** to forward webhooks to localhost:

```bash
stripe listen --forward-to localhost:5000/api/webhooks/billing/stripe
```

### Testing Subscription Creation

```csharp
[Test]
public async Task CreateSubscription_ShouldCreateActiveSubscription()
{
    // Arrange
    var request = new CreateSubscriptionRequest
    {
        TenantId = 1,
        SubscriptionPlanId = 2,
        BillingPeriod = BillingPeriod.Monthly,
        StartTrial = true
    };

    // Act
    var subscription = await _subscriptionService.CreateSubscriptionAsync(request);

    // Assert
    Assert.That(subscription.Status, Is.EqualTo("Trialing"));
    Assert.That(subscription.TrialEndDate, Is.Not.Null);
}
```

---

## Security Considerations

1. **Webhook Signature Verification**
   - Always verify webhook signatures before processing
   - Use Stripe SDK or Paddle SDK for verification

2. **Tenant Isolation**
   - Subscription checks must respect tenant boundaries
   - Users cannot access subscriptions from other tenants

3. **Feature Access Control**
   - Check features at both service and API levels
   - Log feature access attempts for audit

4. **Payment Information**
   - Never store credit card details
   - Rely on billing providers (Stripe/Paddle) for payment processing

---

## Next Steps

1. ✅ **Entities Created** - Subscription, SubscriptionPlan, Feature
2. ✅ **Service Layer** - ISubscriptionService implementation
3. ✅ **Webhook Handlers** - Stripe webhook handler
4. ⏳ **Migration** - Run database migration
5. ⏳ **Seed Data** - Seed default plans and features
6. ⏳ **Frontend Integration** - Create subscription management UI
7. ⏳ **Testing** - Unit tests and integration tests
8. ⏳ **Paddle Integration** - Implement Paddle webhook handler

---

## Files Created

### Entities
- `SmallHR.Core/Entities/Subscription.cs`
- `SmallHR.Core/Entities/SubscriptionPlan.cs`
- `SmallHR.Core/Entities/Feature.cs`

### DTOs
- `SmallHR.Core/DTOs/Subscription/SubscriptionDto.cs`

### Interfaces
- `SmallHR.Core/Interfaces/ISubscriptionService.cs`

### Services
- `SmallHR.Infrastructure/Services/SubscriptionService.cs`
- `SmallHR.API/Services/StripeWebhookHandler.cs`

### Controllers
- `SmallHR.API/Controllers/SubscriptionsController.cs`
- `SmallHR.API/Controllers/BillingWebhooksController.cs`

### Configuration
- `SmallHR.Infrastructure/Data/ApplicationDbContext.cs` (updated)
- `SmallHR.API/Program.cs` (updated)

---

**Status**: ✅ Core implementation complete  
**Next**: Run database migration and seed default plans

