# Tenant Lifecycle Management Guide

This document outlines the complete tenant lifecycle management system, from signup to deletion, with automated provisioning, monitoring, and billing integration.

---

## Overview

Tenant lifecycle management automates the entire tenant journey from signup through deletion, handling provisioning, activation, monitoring, upgrades/downgrades, suspension, and cancellation.

### Lifecycle Stages

1. **Signup / Provisioning** - Tenant creation and initial setup
2. **Activation** - Billing confirmation and feature access
3. **Operation / Monitoring** - Usage tracking and limit enforcement
4. **Upgrade / Downgrade** - Plan changes
5. **Suspension / Cancellation** - Payment failures and termination

---

## 1. Signup / Provisioning

### Overview

When a user signs up, a tenant record is created and automatically provisioned with default roles, settings, and an admin user.

### Flow

```
1. User Signs Up
   ↓
2. Create Tenant Record
   - TenantId, Name, Domain
   - AdminEmail, AdminFirstName, AdminLastName
   - StripeCustomerId (if provided)
   ↓
3. Assign Default Subscription Plan (Free)
   ↓
4. Background Worker Picks Up Provisioning
   ↓
5. Provisioning Steps:
   - Create Subscription
   - Verify Roles
   - Seed Modules
   - Seed Departments & Positions
   - Seed Role Permissions
   - Create Admin User
   - Assign Admin Role
   - Send Welcome Email
   ↓
6. Tenant Status = Active
```

### Signup API

**POST /api/tenantlifecycle/signup**

```http
POST /api/tenantlifecycle/signup
Content-Type: application/json

{
  "tenantName": "Acme Corp",
  "domain": "acme",
  "adminEmail": "admin@acme.com",
  "adminFirstName": "John",
  "adminLastName": "Admin",
  "subscriptionPlanId": 2,
  "startTrial": false,
  "stripeCustomerId": "cus_xxx",
  "idempotencyToken": "unique-token-123"
}
```

**Response:**
```json
{
  "tenantId": 1,
  "message": "Tenant signup initiated. Provisioning will start automatically.",
  "status": "Provisioning"
}
```

### Provisioning Steps

1. **Subscription Creation**
   - Creates subscription (defaults to Free plan if not specified)
   - Links subscription to tenant

2. **Roles Verification**
   - Ensures all required roles exist (SuperAdmin, Admin, HR, Employee)

3. **Modules Seeding**
   - Creates tenant-specific navigation modules (Dashboard, Employees, Organization, etc.)

4. **Departments & Positions Seeding**
   - Creates default organizational structure

5. **Role Permissions Seeding**
   - Sets up role-based access control for all pages

6. **Admin User Creation**
   - Creates tenant admin user account
   - Generates secure temporary password

7. **Admin Role Assignment**
   - Assigns Admin role to tenant admin user

8. **Welcome Email**
   - Sends welcome email with login details and password setup link

### Idempotency

Signup is idempotent - safe to retry with the same `idempotencyToken`:

```http
POST /api/tenantlifecycle/signup
{
  "tenantName": "Acme Corp",
  "idempotencyToken": "unique-token-123",
  ...
}

# If tenant already exists with this token, returns existing tenant
```

---

## 2. Activation

### Overview

Once billing is confirmed (via Stripe/Paddle webhook), the tenant is activated and granted feature access based on their plan.

### Activation via Webhook

**Stripe Webhook: `invoice.payment_succeeded`**

```
1. Stripe sends webhook
   ↓
2. StripeWebhookHandler processes event
   ↓
3. Find subscription by ExternalSubscriptionId or ExternalCustomerId
   ↓
4. Update subscription status to Active
   ↓
5. Activate tenant via TenantLifecycleService
   ↓
6. Grant feature access based on plan
   ↓
7. Record lifecycle event
```

### Manual Activation

**POST /api/tenantlifecycle/{tenantId}/activate**

```http
POST /api/tenantlifecycle/1/activate?externalCustomerId=cus_xxx
Authorization: Bearer {token}
```

**Response:**
```json
{
  "message": "Tenant activated successfully"
}
```

### Activation Process

1. **Update Tenant Status**
   - Set `Status = Active`
   - Set `ActivatedAt = DateTime.UtcNow`
   - Set `IsSubscriptionActive = true`

2. **Update Billing Customer IDs**
   - Set `StripeCustomerId` or `PaddleCustomerId` if provided

3. **Grant Feature Access**
   - Feature access is automatically enabled based on subscription plan

4. **Record Lifecycle Event**
   - Creates `TenantLifecycleEvent` with type `Activated`

---

## 3. Operation / Monitoring

### Overview

Regular monitoring tracks usage metrics, enforces plan limits, and alerts when thresholds are exceeded.

### Background Monitoring

**TenantLifecycleMonitoringHostedService** runs every hour:

1. **Check Usage Limits** - For all active tenants
2. **Process Pending Deletions** - Delete tenants after retention period
3. **Check Grace Periods** - Cancel tenants if grace period expired

### Usage Limits Checked

- **Employee Limit** - Number of employees vs plan limit
- **User Limit** - Number of users vs plan limit
- **Storage Limit** - Storage used vs plan limit
- **API Rate Limit** - API requests vs daily limit

### Alerts

When limits are exceeded, alerts are sent:

```json
{
  "alertType": "employee_limit",
  "message": "Employee limit reached (150/200)",
  "tenantId": 1,
  "tenantName": "Acme Corp"
}
```

**Alert Types:**
- `employee_limit` - Employee limit reached
- `employee_limit_warning` - Employee limit approaching (90%)
- `storage_limit` - Storage limit reached
- `api_rate_limit_warning` - API rate limit approaching (90%)

### Monitoring API

**GET /api/usagemetrics/summary?tenantId=1**

Returns current usage vs limits:
```json
{
  "tenantId": 1,
  "tenantName": "Acme Corp",
  "employeeCount": 150,
  "employeeLimit": 200,
  "storageBytesUsed": 52428800,
  "storageLimitBytes": 1073741824,
  "apiRequestsToday": 500,
  "apiLimitPerDay": 10000
}
```

---

## 4. Upgrade / Downgrade

### Overview

Tenants can switch subscription tiers, with features immediately updated based on the new plan.

### Upgrade Plan

**POST /api/tenantlifecycle/{tenantId}/upgrade**

```http
POST /api/tenantlifecycle/1/upgrade
Content-Type: application/json

{
  "newPlanId": 3
}
```

**Response:**
```json
{
  "message": "Plan upgraded successfully"
}
```

### Downgrade Plan

**POST /api/tenantlifecycle/{tenantId}/downgrade**

```http
POST /api/tenantlifecycle/1/downgrade
Content-Type: application/json

{
  "newPlanId": 2
}
```

### Plan Switch Process

1. **Get Current Plan** - Retrieve current subscription plan
2. **Get New Plan** - Retrieve new subscription plan
3. **Update Subscription** - Change subscription plan
4. **Update Tenant** - Update `SubscriptionPlan` and `MaxEmployees`
5. **Grant/Revoke Features** - Feature access automatically updated
6. **Record Lifecycle Event** - Record upgrade or downgrade event
7. **Notify Tenant Admin** - Send email notification

### Feature Updates

- **Immediate Effect** - Features updated immediately upon plan change
- **No Downtime** - Plan changes don't interrupt service
- **Feature Gating** - `RequireFeatureAttribute` automatically enforces new limits

---

## 5. Suspension / Cancellation

### Overview

Tenants can be suspended (payment failure) or cancelled (voluntary termination), with grace periods and data retention options.

### Suspension

**When:** Payment fails, billing issue, policy violation

**POST /api/tenantlifecycle/{tenantId}/suspend**

```http
POST /api/tenantlifecycle/1/suspend
Content-Type: application/json

{
  "reason": "Payment failed - card declined",
  "gracePeriodDays": 30
}
```

**Suspension Process:**

1. **Update Tenant Status**
   - Set `Status = Suspended`
   - Set `SuspendedAt = DateTime.UtcNow`
   - Set `GracePeriodEndsAt = DateTime.UtcNow.AddDays(30)`
   - Set `IsSubscriptionActive = false`

2. **Block Feature Access**
   - `FeatureAccessMiddleware` prevents access for suspended tenants

3. **Record Lifecycle Event**
   - Creates `TenantLifecycleEvent` with type `Suspended`

4. **Notify Tenant Admin**
   - Sends suspension email with grace period information

### Resume

**POST /api/tenantlifecycle/{tenantId}/resume**

```http
POST /api/tenantlifecycle/1/resume
Authorization: Bearer {token}
```

**Resume Process:**

1. **Update Tenant Status**
   - Set `Status = Active`
   - Clear `SuspendedAt` and `GracePeriodEndsAt`
   - Set `IsSubscriptionActive = true`

2. **Restore Feature Access**
   - Features automatically restored

3. **Record Lifecycle Event**
   - Creates `TenantLifecycleEvent` with type `Resumed`

4. **Notify Tenant Admin**
   - Sends resume confirmation email

### Cancellation

**When:** Tenant requests cancellation, grace period expires

**POST /api/tenantlifecycle/{tenantId}/cancel**

```http
POST /api/tenantlifecycle/1/cancel
Content-Type: application/json

{
  "reason": "Switching to another provider",
  "scheduleDeletion": true,
  "retentionDays": 90
}
```

**Cancellation Process:**

1. **Update Tenant Status**
   - Set `Status = Cancelled` (or `PendingDeletion` if `scheduleDeletion = true`)
   - Set `CancelledAt = DateTime.UtcNow`
   - Set `ScheduledDeletionAt = DateTime.UtcNow.AddDays(90)` if scheduled
   - Set `IsSubscriptionActive = false`

2. **Cancel Subscription**
   - Cancel subscription via `SubscriptionService`

3. **Block Feature Access**
   - Features immediately disabled

4. **Record Lifecycle Event**
   - Creates `TenantLifecycleEvent` with type `Cancelled`

5. **Send Cancellation Email**
   - Includes data export link
   - Notifies about retention period

6. **Schedule Deletion** (if enabled)
   - Tenant marked for deletion after retention period
   - Background worker processes deletions

### Grace Period

**Grace Period Logic:**

- **Suspended Tenants** - 30 days grace period (default, configurable)
- **Grace Period Expiry** - Automatically cancels tenant if payment not recovered
- **Grace Period Monitoring** - Background worker checks daily

**Grace Period End:**

```
GracePeriodEndsAt <= DateTime.UtcNow
  ↓
Auto-cancel tenant
  ↓
Send cancellation email
  ↓
Schedule deletion (90 days retention)
```

---

## 6. Data Export

### Overview

Tenants can export their data before deletion or cancellation.

### Export Data

**GET /api/tenantlifecycle/{tenantId}/export**

```http
GET /api/tenantlifecycle/1/export
Authorization: Bearer {token}
```

**Response:**
- **Content-Type:** `application/json`
- **File Name:** `tenant-1-export-20251102120000.json`
- **Content:** All tenant data (employees, leave requests, attendance, etc.)

### Export Contents

```json
{
  "exportedAt": "2025-11-02T12:00:00Z",
  "tenantId": 1,
  "tenantName": "Acme Corp",
  "tenantDomain": "acme",
  "employees": [...],
  "leaveRequests": [...],
  "attendances": [...],
  "departments": [...],
  "positions": [...],
  "modules": [...],
  "rolePermissions": [...]
}
```

---

## 7. Soft Delete & Retention

### Overview

Cancelled tenants are soft-deleted (marked for deletion) with a retention period before permanent deletion.

### Soft Delete Process

```
1. Tenant Cancelled
   ↓
2. Status = PendingDeletion
   ↓
3. ScheduledDeletionAt = Now + 90 days
   ↓
4. Data Retention Period (90 days)
   ↓
5. Background Worker Processes Deletions
   ↓
6. Status = Deleted (hard delete)
```

### Retention Period

- **Default:** 90 days
- **Configurable:** Per tenant cancellation request
- **Purpose:** Allow data recovery or export

### Deletion Processing

**TenantLifecycleMonitoringHostedService** processes pending deletions:

```csharp
// Runs every hour
var tenantsToDelete = await _context.Tenants
    .Where(t => t.Status == TenantStatus.PendingDeletion &&
               t.ScheduledDeletionAt.HasValue &&
               t.ScheduledDeletionAt.Value <= DateTime.UtcNow)
    .ToListAsync();

foreach (var tenant in tenantsToDelete)
{
    await lifecycleService.HardDeleteTenantAsync(tenant.Id);
}
```

### Hard Delete

Permanently deletes tenant and all related data:

1. **Record Lifecycle Event** - Record deletion event
2. **Delete Tenant** - Cascade deletes all related data
3. **Log Deletion** - Log permanent deletion

---

## 8. Lifecycle Events

### Overview

All tenant lifecycle changes are recorded as events for audit and tracking.

### Event Types

- `Created` - Tenant created
- `ProvisioningStarted` - Provisioning initiated
- `ProvisioningCompleted` - Provisioning completed
- `ProvisioningFailed` - Provisioning failed
- `Activated` - Tenant activated (billing confirmed)
- `Suspended` - Tenant suspended
- `Resumed` - Tenant resumed
- `Upgraded` - Plan upgraded
- `Downgraded` - Plan downgraded
- `Cancelled` - Tenant cancelled
- `MarkedForDeletion` - Tenant marked for deletion
- `Deleted` - Tenant permanently deleted
- `PaymentFailed` - Payment failed
- `PaymentRecovered` - Payment recovered
- `GracePeriodStarted` - Grace period started
- `GracePeriodExpired` - Grace period expired

### Get Lifecycle Events

**GET /api/tenantlifecycle/{tenantId}/events?limit=100**

```json
[
  {
    "id": 1,
    "tenantId": 1,
    "eventType": "Created",
    "previousStatus": "Provisioning",
    "newStatus": "Provisioning",
    "reason": "Tenant created via signup",
    "triggeredBy": "system",
    "eventDate": "2025-11-01T10:00:00Z",
    "metadata": {
      "planId": 1
    }
  },
  {
    "id": 2,
    "tenantId": 1,
    "eventType": "Activated",
    "previousStatus": "Provisioning",
    "newStatus": "Active",
    "reason": "Tenant activated (billing confirmed)",
    "triggeredBy": "system",
    "eventDate": "2025-11-01T10:05:00Z"
  }
]
```

---

## 9. Webhook Integration

### Stripe Webhooks

**Webhook Endpoint:** `POST /api/webhooks/billing/stripe`

**Supported Events:**
- `customer.subscription.created` - Subscription created
- `customer.subscription.updated` - Subscription updated
- `customer.subscription.deleted` - Subscription deleted
- `invoice.payment_succeeded` - Payment succeeded → Activate tenant
- `invoice.payment_failed` - Payment failed → Suspend tenant
- `customer.subscription.trial_will_end` - Trial ending soon

### Payment Succeeded → Activation

```
1. Stripe sends webhook: invoice.payment_succeeded
   ↓
2. StripeWebhookHandler.HandlePaymentSucceededAsync()
   ↓
3. Find subscription by ExternalSubscriptionId
   ↓
4. Update subscription status to Active
   ↓
5. Activate tenant via TenantLifecycleService
   ↓
6. Grant feature access
   ↓
7. Record lifecycle event: Activated
```

### Payment Failed → Suspension

```
1. Stripe sends webhook: invoice.payment_failed
   ↓
2. StripeWebhookHandler.HandlePaymentFailedAsync()
   ↓
3. Find subscription by ExternalSubscriptionId
   ↓
4. Update subscription status to PastDue
   ↓
5. Suspend tenant via TenantLifecycleService
   ↓
6. Set grace period (30 days)
   ↓
7. Block feature access
   ↓
8. Record lifecycle event: Suspended
   ↓
9. Send suspension email to tenant admin
```

---

## 10. Background Jobs

### TenantLifecycleMonitoringHostedService

**Schedule:** Every 1 hour

**Tasks:**
1. **Check Usage Limits** - For all active tenants
2. **Process Pending Deletions** - Delete tenants after retention period
3. **Check Grace Periods** - Cancel tenants if grace period expired

### TenantProvisioningHostedService

**Schedule:** Every 10 seconds

**Tasks:**
- Monitor tenants with `Status = Provisioning`
- Process up to 5 tenants at a time
- Complete provisioning steps
- Update status to `Active` or `ProvisioningFailed`

---

## 11. API Reference

### Tenant Lifecycle Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/tenantlifecycle/signup` | Sign up new tenant |
| POST | `/api/tenantlifecycle/{id}/activate` | Activate tenant |
| POST | `/api/tenantlifecycle/{id}/upgrade` | Upgrade plan |
| POST | `/api/tenantlifecycle/{id}/downgrade` | Downgrade plan |
| POST | `/api/tenantlifecycle/{id}/suspend` | Suspend tenant |
| POST | `/api/tenantlifecycle/{id}/resume` | Resume tenant |
| POST | `/api/tenantlifecycle/{id}/cancel` | Cancel tenant |
| GET | `/api/tenantlifecycle/{id}/export` | Export tenant data |
| GET | `/api/tenantlifecycle/{id}/events` | Get lifecycle events |
| GET | `/api/tenantlifecycle/{id}/suspension-info` | Get suspension info |

---

## 12. Lifecycle State Machine

### State Transitions

```
Provisioning
    ↓ (success)
Active
    ↓ (payment failed)
Suspended
    ↓ (payment recovered)
Active
    ↓ (cancelled)
Cancelled → PendingDeletion → Deleted
```

### Valid Transitions

| From | To | Trigger |
|------|-----|---------|
| Provisioning | Active | Provisioning completed |
| Provisioning | ProvisioningFailed | Provisioning failed |
| Active | Suspended | Payment failed |
| Suspended | Active | Payment recovered |
| Suspended | Cancelled | Grace period expired |
| Active | Cancelled | Tenant cancellation |
| Cancelled | PendingDeletion | Schedule deletion |
| PendingDeletion | Deleted | Retention period expired |

---

## 13. Best Practices

### Signup
- ✅ Use idempotency tokens for retry safety
- ✅ Validate tenant name/domain uniqueness
- ✅ Create subscription during signup
- ✅ Background provisioning for scalability

### Activation
- ✅ Activate via webhook (automated)
- ✅ Fallback to manual activation if needed
- ✅ Record lifecycle events for audit

### Monitoring
- ✅ Check limits regularly (hourly)
- ✅ Alert at 90% usage (proactive)
- ✅ Enforce limits at 100% (hard stop)

### Suspension
- ✅ Grace period (30 days default)
- ✅ Clear communication to tenant
- ✅ Easy reactivation process

### Cancellation
- ✅ Data retention (90 days default)
- ✅ Export option before deletion
- ✅ Automated deletion after retention

---

## Summary

### Implemented Features

1. ✅ **Signup / Provisioning** - Automated tenant creation and setup
2. ✅ **Activation** - Webhook-based activation on billing confirmation
3. ✅ **Operation / Monitoring** - Usage tracking and limit enforcement
4. ✅ **Upgrade / Downgrade** - Plan changes with immediate feature updates
5. ✅ **Suspension / Cancellation** - Payment failures and termination handling
6. ✅ **Data Export** - Tenant data export before deletion
7. ✅ **Soft Delete & Retention** - Graceful deletion with retention period
8. ✅ **Lifecycle Events** - Complete audit trail
9. ✅ **Background Jobs** - Automated monitoring and processing
10. ✅ **Webhook Integration** - Stripe/Paddle billing integration

### Automation

- **No Manual Intervention** - All lifecycle stages are automated
- **Webhook-Driven** - Billing events trigger lifecycle changes
- **Background Processing** - Monitoring and cleanup run automatically
- **Graceful Failures** - Errors are handled with retries and notifications

---

**Last Updated:** 2025-11-02  
**Version:** 1.0

