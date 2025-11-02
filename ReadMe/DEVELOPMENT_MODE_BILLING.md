# Development Mode - Billing Configuration

## Overview

SmallHR's subscription and billing system is designed to work in **development mode without requiring Stripe or Paddle configuration**. You can create and manage subscriptions locally without setting up external payment providers.

---

## Development Mode Setup

### 1. No Stripe Configuration Required

In development, you can skip Stripe configuration entirely. The system will:

- ✅ Create subscriptions with `BillingProvider = None`
- ✅ Allow subscription management without external IDs
- ✅ Process webhooks gracefully (even without signature verification)
- ✅ Enable all subscription features locally

### 2. Creating Subscriptions (Development)

When creating subscriptions in development, you don't need to provide:

- ❌ Stripe Customer ID
- ❌ Stripe Subscription ID
- ❌ Paddle Customer ID
- ❌ External billing provider IDs

Simply create subscriptions directly via the API:

```http
POST /api/subscriptions
Content-Type: application/json
Authorization: Bearer {token}

{
  "tenantId": 1,
  "subscriptionPlanId": 2,
  "billingPeriod": "Monthly",
  "startTrial": false
}
```

The subscription will be created with:
- `BillingProvider = None`
- `ExternalSubscriptionId = null`
- `ExternalCustomerId = null`
- `Status = Active` (or `Trialing` if trial is enabled)

---

## Subscription Management in Development

### Create Subscription Without Payment

```csharp
// Via API
POST /api/subscriptions
{
  "tenantId": 1,
  "subscriptionPlanId": 2,  // Basic plan
  "billingPeriod": "Monthly",
  "startTrial": false
}
```

### Update Subscription

```http
PUT /api/subscriptions/{id}
{
  "subscriptionPlanId": 3,  // Upgrade to Pro
  "billingPeriod": "Yearly",
  "autoRenew": true
}
```

### Cancel Subscription

```http
POST /api/subscriptions/{id}/cancel?reason=Testing
```

### Reactivate Subscription

```http
POST /api/subscriptions/{id}/reactivate
```

---

## Webhook Handling (Development)

### Stripe Webhooks (Optional in Dev)

In development, the Stripe webhook endpoint (`/api/webhooks/billing/stripe`) will:

- ✅ Accept webhook events without signature verification
- ✅ Log warnings instead of errors when Stripe is not configured
- ✅ Return success to prevent webhook retries
- ✅ Process webhook events if provided (for testing)

**Note**: If you want to test webhooks locally without Stripe:

1. **Use Stripe CLI** (optional):
   ```bash
   stripe listen --forward-to localhost:5000/api/webhooks/billing/stripe
   ```

2. **Or simulate webhook events manually**:
   ```bash
   curl -X POST http://localhost:5000/api/webhooks/billing/stripe \
     -H "Content-Type: application/json" \
     -H "Stripe-Signature: test" \
     -d '{"type": "customer.subscription.created", "data": {...}}'
   ```

---

## appsettings.json Configuration

### Minimum Configuration (Development)

You can leave Stripe/Paddle configuration empty:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Jwt": {
    "Key": "..."
  }
}
```

### Optional: Add Placeholder Configuration

If you want to avoid warnings in logs:

```json
{
  "Stripe": {
    "ApiKey": "sk_test_development_only",
    "WebhookSecret": "whsec_development_only"
  }
}
```

**Note**: These values won't be validated in development mode.

---

## Testing Subscriptions

### 1. Create a Subscription Plan

First, seed subscription plans (or create via database):

```sql
INSERT INTO SubscriptionPlans (Name, MonthlyPrice, MaxEmployees, IsActive, IsVisible, DisplayOrder)
VALUES 
  ('Free', 0, 10, 1, 1, 1),
  ('Basic', 99, 50, 1, 1, 2),
  ('Pro', 299, 200, 1, 1, 3),
  ('Enterprise', 999, 1000, 1, 1, 4);
```

### 2. Create a Subscription

```http
POST /api/subscriptions
Authorization: Bearer {superadmin_token}

{
  "tenantId": 1,
  "subscriptionPlanId": 2,
  "billingPeriod": "Monthly",
  "startTrial": false
}
```

### 3. Test Feature Checking

```http
GET /api/subscriptions/features
Authorization: Bearer {token}
```

### 4. Test Limits

```csharp
// In EmployeeService or similar
var canAddEmployee = await _subscriptionService.CheckEmployeeLimitAsync(tenantId);
var maxEmployees = await _subscriptionService.GetMaxEmployeesAsync(tenantId);
```

---

## Development Workflow

### Recommended Flow

1. **Create Tenant** (if not exists)
   ```http
   POST /api/tenants
   ```

2. **Create Subscription Plan** (seed or via database)

3. **Create Subscription for Tenant**
   ```http
   POST /api/subscriptions
   {
     "tenantId": 1,
     "subscriptionPlanId": 2,
     "billingPeriod": "Monthly"
   }
   ```

4. **Test Subscription Features**
   - Check limits
   - Verify feature access
   - Test upgrades/downgrades

5. **Test Subscription Management**
   - Cancel subscription
   - Reactivate subscription
   - Update subscription plan

---

## Migration to Production

When moving to production with Stripe:

1. **Configure Stripe Settings**:
   ```json
   {
     "Stripe": {
       "ApiKey": "sk_live_...",
       "WebhookSecret": "whsec_...",
       "PublishableKey": "pk_live_..."
     }
   }
   ```

2. **Update Webhook Endpoint** in Stripe Dashboard

3. **Update Subscriptions**:
   - Link existing subscriptions to Stripe customer/subscription IDs
   - Or migrate subscriptions through Stripe checkout

4. **Enable Signature Verification** in webhook handler

---

## Troubleshooting

### Subscription Created but Features Not Working

**Check**:
- Subscription status is `Active` or `Trialing`
- Subscription plan has features assigned
- Tenant ID is correctly resolved

### Webhook Errors in Development

**Solution**: Ignore webhook errors in development. They're logged but won't break the system.

### Cannot Create Subscription

**Check**:
- Tenant exists
- Subscription plan exists and is active
- Tenant doesn't already have an active subscription

---

## Summary

✅ **Development Mode**: Works without Stripe/Paddle  
✅ **Subscription Management**: Full CRUD operations available  
✅ **Feature Checking**: All feature checks work locally  
✅ **Webhooks**: Optional, gracefully handles missing config  
✅ **Production Ready**: Easy migration path when ready  

**Next Step**: Create your first subscription and test the system!

