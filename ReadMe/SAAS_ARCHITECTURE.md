# SaaS Architecture Guide

This document outlines the complete SaaS architecture for SmallHR, including tenant management, subscription handling, feature access control, and provisioning.

---

## Architecture Overview

SmallHR follows a **Single Database, Shared Schema (Soft Isolation)** multi-tenant architecture with subscription-based feature access control.

### Core Components

1. **Tenant Table** - Company info, subscription tier, status
2. **Subscription Table** - Plan type, limits, start/end date
3. **User Table** - Includes TenantId, Role
4. **Feature Access Middleware** - Checks tenant's plan before allowing API calls
5. **Tenant Provisioning Service** - Handles DB setup and initial data

---

## 1. Tenant Table

### Entity Structure

```csharp
public class Tenant : BaseEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Domain { get; set; }
    
    // Subscription info (denormalized for quick access)
    public required string SubscriptionPlan { get; set; }
    public int? MaxEmployees { get; set; }
    public bool IsSubscriptionActive { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    
    // Provisioning status
    public TenantStatus Status { get; set; } = TenantStatus.Provisioning;
    
    // Admin user info
    public string? AdminEmail { get; set; }
    public string? AdminFirstName { get; set; }
    public string? AdminLastName { get; set; }
}
```

### Tenant Status Values

- `Provisioning` - Tenant setup in progress
- `Active` - Tenant is active and ready
- `Suspended` - Tenant is suspended (billing issue, etc.)
- `Deleted` - Tenant is marked for deletion

### Usage

**Create Tenant:**
```http
POST /api/tenants
{
  "name": "Acme Corp",
  "domain": "acme",
  "adminEmail": "admin@acme.com",
  "adminFirstName": "John",
  "adminLastName": "Admin",
  "subscriptionPlanId": 2,
  "startTrial": false
}
```

**Get Tenant:**
```http
GET /api/tenants/{id}
```

**Update Tenant:**
```http
PUT /api/tenants/{id}
{
  "name": "Acme Corporation",
  "domain": "acme"
}
```

---

## 2. Subscription Table

### Entity Structure

```csharp
public class Subscription : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int SubscriptionPlanId { get; set; }
    
    // Subscription details
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    
    // Dates
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialStartDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    
    // External provider integration
    public string? ExternalSubscriptionId { get; set; }
    public string? ExternalCustomerId { get; set; }
    public BillingProvider? BillingProvider { get; set; }
    
    // Cancellation
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    
    // Navigation
    public virtual Tenant Tenant { get; set; }
    public virtual SubscriptionPlan Plan { get; set; }
}
```

### Subscription Status Values

- `Active` - Subscription is active
- `Trial` - Subscription is in trial period
- `Cancelled` - Subscription is cancelled
- `Expired` - Subscription has expired
- `PastDue` - Subscription payment is past due
- `Suspended` - Subscription is suspended

### Billing Periods

- `Monthly` - Monthly billing
- `Quarterly` - Quarterly billing (3 months)
- `Yearly` - Annual billing (12 months)

### Usage

**Create Subscription:**
```http
POST /api/subscriptions
{
  "tenantId": 1,
  "subscriptionPlanId": 2,
  "billingPeriod": "Monthly",
  "startTrial": false
}
```

**Get Subscription:**
```http
GET /api/subscriptions/tenant/{tenantId}
```

**Update Subscription:**
```http
PUT /api/subscriptions/{id}
{
  "subscriptionPlanId": 3,
  "billingPeriod": "Yearly"
}
```

**Cancel Subscription:**
```http
POST /api/subscriptions/{id}/cancel
{
  "reason": "Switching to another provider"
}
```

---

## 3. User Table

### Entity Structure

```csharp
public class User : IdentityUser
{
    // Tenant association
    public string? TenantId { get; set; }
    
    // User info
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    
    // Address
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public string Country { get; set; }
    
    // Status
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Refresh token
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    // Navigation
    public virtual ICollection<Employee> Employees { get; set; }
}
```

### Key Features

1. **TenantId Association** - Links user to tenant
2. **Role-Based Access** - Uses ASP.NET Core Identity roles
3. **Multi-Tenant Support** - Users belong to specific tenants

### Usage

**Create User:**
```http
POST /api/users
{
  "email": "user@acme.com",
  "password": "SecurePassword123!",
  "firstName": "Jane",
  "lastName": "Doe",
  "tenantId": "acme",
  "roles": ["Employee"]
}
```

**Get Users:**
```http
GET /api/users
# Returns users for current tenant
```

**Assign Role:**
```http
POST /api/users/{id}/roles
{
  "roles": ["HR", "Employee"]
}
```

---

## 4. Feature Access Middleware

### Overview

The Feature Access Middleware (`FeatureAccessMiddleware`) checks the tenant's subscription status and active features before allowing API calls.

### Implementation

**Location:** `SmallHR.API/Middleware/FeatureAccessMiddleware.cs`

**Pipeline Order:**
```
1. TenantResolutionMiddleware - Resolves tenant from request
2. FeatureAccessMiddleware - Checks subscription status
3. TenantRateLimitMiddleware - Enforces rate limits
4. Authorization - Role-based access control
```

### Features

1. **Subscription Status Check** - Verifies subscription is active
2. **Feature Verification** - Uses `RequireFeatureAttribute` for endpoint-specific checks
3. **Graceful Failures** - Returns appropriate error messages
4. **Bypass Rules** - Skips checks for auth, subscriptions, billing endpoints

### Middleware Flow

```
Request → Tenant Resolution → Feature Check → Rate Limit → Authorization → Endpoint
                                  ↓
                         Subscription Active?
                                  ↓
                         Feature Available?
                                  ↓
                         Allow Request / Deny (403)
```

### Configuration

**In Program.cs:**
```csharp
app.UseMiddleware<FeatureAccessMiddleware>();
```

**Exempted Paths:**
- `/health` - Health checks
- `/api/auth` - Authentication endpoints
- `/api/subscriptions` - Subscription management
- `/api/billing` - Billing endpoints
- `/swagger` - API documentation
- `/api/dev` - Development endpoints
- `/api/webhooks` - Webhook endpoints

---

## 5. RequireFeature Attribute

### Overview

The `RequireFeatureAttribute` marks endpoints that require specific subscription plan features.

### Usage

**On Controller:**
```csharp
[ApiController]
[Route("api/[controller]")]
[RequireFeature("advanced-reports", "custom-dashboards")]
public class ReportsController : ControllerBase
{
    // All endpoints require features
}
```

**On Endpoint:**
```csharp
[HttpPost("generate-report")]
[RequireFeature("advanced-reports")]
public async Task<ActionResult> GenerateReport(...)
{
    // Endpoint requires "advanced-reports" feature
}
```

**Multiple Features:**
```csharp
[RequireFeature("advanced-reports", "export-pdf", "custom-filters")]
public async Task<ActionResult> GenerateAdvancedReport(...)
{
    // Requires all three features
}
```

### Feature Keys

Common feature keys:
- `basic-reports` - Basic reporting
- `advanced-reports` - Advanced reporting
- `custom-dashboards` - Custom dashboards
- `api-access` - API access
- `bulk-operations` - Bulk operations
- `custom-branding` - Custom branding
- `sso` - Single Sign-On
- `audit-logs` - Audit logging
- `data-export` - Data export
- `advanced-permissions` - Advanced permissions

### Error Response

When feature is not available:

```json
{
  "error": "Feature not available",
  "message": "Feature 'advanced-reports' is not available in your current plan (Basic). Please upgrade to access this feature.",
  "requiredFeature": "advanced-reports",
  "currentPlan": "Basic"
}
```

**HTTP Status:** `403 Forbidden`

---

## 6. Tenant Provisioning Service

### Overview

The Tenant Provisioning Service (`ITenantProvisioningService`) handles automated tenant setup, including database configuration and initial data seeding.

### Implementation

**Location:** `SmallHR.Infrastructure/Services/TenantProvisioningService.cs`

### Provisioning Steps

1. **Subscription Creation** - Creates subscription (defaults to Free plan)
2. **Roles Verification** - Ensures all roles exist
3. **Modules Seeding** - Creates tenant-specific navigation modules
4. **Departments & Positions Seeding** - Creates default organizational structure
5. **Role Permissions Seeding** - Sets up role-based access control
6. **Admin User Creation** - Creates tenant admin user
7. **Admin Role Assignment** - Assigns Admin role
8. **Welcome Email** - Sends welcome email with login details

### Usage

**Automatic Provisioning (Background Worker):**
```csharp
// Tenant created with Status = Provisioning
// Background worker picks it up automatically
POST /api/tenants
{
  "name": "Acme Corp",
  "adminEmail": "admin@acme.com",
  ...
}
```

**Manual Provisioning (Synchronous):**
```http
POST /api/provisioning/{tenantId}
{
  "subscriptionPlanId": 2,
  "startTrial": false
}
```

### Provisioning Result

```json
{
  "success": true,
  "tenantId": 1,
  "stepsCompleted": [
    "Subscription created",
    "Roles verified",
    "Modules seeded",
    "Departments & positions seeded",
    "Role permissions seeded",
    "Admin user created",
    "Admin role assigned",
    "Welcome email sent"
  ],
  "adminUser": {
    "email": "admin@acme.com",
    "passwordToken": "..."
  },
  "subscription": {
    "plan": "Free",
    "status": "Active"
  }
}
```

---

## Complete Request Flow

### Example: Access Protected Endpoint

```
1. Request → GET /api/reports/advanced
   Headers: Authorization: Bearer {token}
            X-Tenant-Id: acme

2. TenantResolutionMiddleware
   - Extracts tenant ID from JWT or header
   - Sets tenant context: "acme"

3. FeatureAccessMiddleware
   - Looks up tenant: "acme" → TenantId: 1
   - Gets subscription: TenantId 1 → Subscription
   - Checks subscription status: Active ✓

4. RequireFeatureAttribute
   - Gets required features: ["advanced-reports"]
   - Checks feature: HasFeature(1, "advanced-reports")
   - Verifies plan has feature: ✓

5. Authorization
   - Checks user roles: ["Admin"] ✓
   - Verifies permissions: ✓

6. Endpoint Execution
   - Processes request
   - Returns response
```

### Example: Feature Not Available

```
1. Request → GET /api/reports/advanced
   Headers: Authorization: Bearer {token}
            X-Tenant-Id: acme

2. TenantResolutionMiddleware
   - Sets tenant context: "acme"

3. FeatureAccessMiddleware
   - Checks subscription status: Active ✓

4. RequireFeatureAttribute
   - Gets required features: ["advanced-reports"]
   - Checks feature: HasFeature(1, "advanced-reports")
   - Feature not available in Basic plan ✗

5. Response → 403 Forbidden
   {
     "error": "Feature not available",
     "message": "Feature 'advanced-reports' is not available in your current plan (Basic)...",
     "requiredFeature": "advanced-reports",
     "currentPlan": "Basic"
   }
```

---

## Database Schema

### Entity Relationships

```
Tenant (1) ──── (1) Subscription ──── (1) SubscriptionPlan
  │                                              │
  │                                              │
  │                                              │
  │                                    SubscriptionPlanFeature (Many-to-Many)
  │                                              │
  │                                              │
  │                                              │
User (Many) ──── (1) Tenant                    Feature
  │
  │
  │
Employee (Many) ──── (1) Tenant
  │
  │
LeaveRequest (Many)
Attendance (Many)
Department (Many) ──── (1) Tenant
Position (Many) ──── (1) Tenant
Module (Many) ──── (1) Tenant
RolePermission (Many) ──── (1) Tenant
TenantUsageMetrics (1) ──── (1) Tenant
```

---

## Best Practices

### 1. Tenant Resolution
- ✅ Always use tenant resolution middleware
- ✅ Enforce tenant boundary checks
- ✅ Validate tenant ID in JWT claims
- ✅ Support multiple resolution methods (subdomain, header, JWT)

### 2. Feature Access
- ✅ Use `RequireFeatureAttribute` for feature-gated endpoints
- ✅ Check subscription status before feature checks
- ✅ Provide clear error messages for missing features
- ✅ Log feature access denials

### 3. Subscription Management
- ✅ Create subscription during tenant provisioning
- ✅ Sync subscription status from billing provider
- ✅ Handle subscription lifecycle (trial, active, cancelled, expired)
- ✅ Update tenant status based on subscription

### 4. Provisioning
- ✅ Use background worker for async provisioning
- ✅ Track provisioning steps and status
- ✅ Handle provisioning failures gracefully
- ✅ Ensure idempotent provisioning (safe to retry)

### 5. Security
- ✅ Row-level security (RLS) via query filters
- ✅ Tenant ID validation on all mutations
- ✅ Rate limiting per tenant
- ✅ Usage metrics tracking
- ✅ Audit logging for tenant operations

---

## Example: Complete Endpoint with Feature Check

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdvancedReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;
    
    public AdvancedReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }
    
    [HttpGet("generate")]
    [RequireFeature("advanced-reports")]
    public async Task<ActionResult<ReportDto>> GenerateReport([FromQuery] ReportRequest request)
    {
        // Feature check ensures tenant has "advanced-reports" feature
        // If not, RequireFeatureAttribute returns 403 before this executes
        
        var report = await _reportsService.GenerateAdvancedReportAsync(request);
        return Ok(report);
    }
    
    [HttpGet("export")]
    [RequireFeature("advanced-reports", "data-export")]
    public async Task<ActionResult> ExportReport([FromQuery] int reportId)
    {
        // Requires both "advanced-reports" AND "data-export" features
        var file = await _reportsService.ExportReportAsync(reportId);
        return File(file, "application/pdf", $"report-{reportId}.pdf");
    }
}
```

---

## Summary

### Implemented Components

1. ✅ **Tenant Table** - Company info, subscription tier, status
2. ✅ **Subscription Table** - Plan type, limits, start/end date
3. ✅ **User Table** - Includes TenantId, Role
4. ✅ **Feature Access Middleware** - Checks tenant's plan before allowing API calls
5. ✅ **Tenant Provisioning Service** - Handles DB setup and initial data

### Key Features

- Multi-tenant architecture with soft isolation
- Subscription-based feature access control
- Automated tenant provisioning
- Role-based access control (RBAC)
- Usage metrics and limit enforcement
- Rate limiting per tenant
- Comprehensive security features

### Next Steps

1. Configure feature flags for subscription plans
2. Set up billing provider integration (Stripe/Paddle)
3. Enable tenant-based rate limiting
4. Implement usage metrics dashboards
5. Set up monitoring and alerting

---

**Last Updated:** 2025-11-02  
**Version:** 1.0

