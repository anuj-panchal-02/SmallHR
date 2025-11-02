# Tenant Administration Console

## Overview

The Tenant Administration Console provides SuperAdmin with comprehensive tenant management capabilities, including tenant impersonation, subscription management, and system-wide metrics.

## Features

### 1. Tenant Management
- **View All Tenants**: List tenants with filters, pagination, and sorting
- **Tenant Details**: Get comprehensive tenant information including users, employees, subscriptions, and usage metrics
- **Suspend/Resume Tenants**: Manage tenant lifecycle states
- **Delete Tenants**: Soft delete or hard delete tenants

### 2. Tenant Impersonation
- **Impersonate Tenant**: Generate short-lived JWT token to view as a specific tenant
- **Impersonation Token**: Special JWT with `IsImpersonating` claim that allows SuperAdmin to act as tenant
- **Stop Impersonation**: Clear impersonation context and return to SuperAdmin view
- **Audit Logging**: All impersonation actions are logged in AdminAudit

### 3. Subscription Management
- **View Subscription History**: Get all subscriptions for a tenant
- **Extend Trial**: Manually extend trial period for subscriptions
- **Change Plan**: Upgrade or downgrade subscription plans manually
- **Payment History**: Track subscription payments and billing history

### 4. System-Wide Metrics
- **Overview Metrics**: Total tenants, active tenants, revenue, usage statistics
- **Revenue Trends**: Monthly recurring revenue (MRR) and annual recurring revenue (ARR) trends
- **Tenant Growth**: Track tenant acquisition over time
- **Churn Analysis**: Analyze subscription cancellations and churn rate

## API Endpoints

### Tenant Management

#### Get All Tenants
```
GET /api/admin/tenants
Query Parameters:
  - search: string (optional) - Search by name, domain, or admin email
  - status: TenantStatus (optional) - Filter by tenant status
  - subscriptionStatus: SubscriptionStatus (optional) - Filter by subscription status
  - pageNumber: int (default: 1)
  - pageSize: int (default: 20)
  - sortBy: string (default: "createdAt") - Options: "name", "createdAt", "status"
  - sortOrder: string (default: "desc") - Options: "asc", "desc"
```

#### Get Tenant Details
```
GET /api/admin/tenants/{id}
Returns:
  - Tenant information
  - User count and employee count
  - Usage metrics
  - Subscription history
  - Recent lifecycle events
```

#### Impersonate Tenant
```
POST /api/admin/tenants/{id}/impersonate
Query Parameters:
  - durationMinutes: int (default: 30) - Token expiration time in minutes
Returns:
  - impersonationToken: string - Short-lived JWT token
  - tenant: object - Tenant information
  - expiresAt: datetime - Token expiration time
  - banner: string - Message to display in UI
```

#### Stop Impersonation
```
POST /api/admin/tenants/stop-impersonation
Clears impersonation context
```

#### Suspend Tenant
```
POST /api/admin/tenants/{id}/suspend
Body:
  {
    "reason": "string (optional)"
  }
```

#### Resume Tenant
```
POST /api/admin/tenants/{id}/resume
Reactivates a suspended tenant
```

#### Delete Tenant
```
DELETE /api/admin/tenants/{id}
Query Parameters:
  - hardDelete: bool (default: false) - If true, permanently deletes tenant
```

### Subscription Management

#### Get Tenant Subscriptions
```
GET /api/admin/subscriptions/tenant/{tenantId}
Returns subscription history for a tenant
```

#### Extend Trial
```
POST /api/admin/subscriptions/{subscriptionId}/extend-trial
Body:
  {
    "extendByDays": int,
    "reason": "string (optional)"
  }
```

#### Change Plan
```
POST /api/admin/subscriptions/{subscriptionId}/change-plan
Body:
  {
    "newPlanId": int,
    "billingPeriod": BillingPeriod,
    "reason": "string (optional)"
  }
```

### Metrics and Analytics

#### Get Overview Metrics
```
GET /api/admin/metrics/overview
Query Parameters:
  - startDate: DateTime (optional) - Default: 1 month ago
  - endDate: DateTime (optional) - Default: now
Returns:
  - Tenants: total, active, suspended, new
  - Revenue: MRR, ARR, active subscriptions
  - Usage: total users, employees, API requests
  - Churn: count, lost revenue, churned subscriptions
```

#### Get Revenue Trends
```
GET /api/admin/metrics/revenue-trends
Query Parameters:
  - months: int (default: 12) - Number of months to analyze
Returns monthly revenue trends
```

#### Get Tenant Growth
```
GET /api/admin/metrics/tenant-growth
Query Parameters:
  - months: int (default: 12) - Number of months to analyze
Returns tenant acquisition trends
```

#### Get Churn Analysis
```
GET /api/admin/metrics/churn-analysis
Query Parameters:
  - months: int (default: 12) - Number of months to analyze
Returns:
  - Total churned subscriptions
  - Lost revenue
  - Churn rate
  - Churn by plan
  - Recent churn events
```

## Impersonation Flow

### 1. Start Impersonation
1. SuperAdmin calls `POST /api/admin/tenants/{id}/impersonate`
2. API generates impersonation JWT token with:
   - `TenantId`: Target tenant ID
   - `IsImpersonating`: "true"
   - `OriginalUserId`: SuperAdmin user ID
   - `OriginalEmail`: SuperAdmin email
   - Short expiration (default: 30 minutes)
3. Token is returned to SuperAdmin
4. SuperAdmin uses this token for subsequent requests

### 2. Using Impersonation Token
- Include impersonation token in `Authorization` header: `Bearer {impersonationToken}`
- Middleware detects `IsImpersonating` claim and sets tenant context to impersonated tenant
- SuperAdmin now sees tenant's data as if they were a tenant user
- All actions are still logged with original SuperAdmin identity

### 3. Stop Impersonation
1. SuperAdmin calls `POST /api/admin/tenants/stop-impersonation`
2. Token is invalidated (or expires naturally)
3. SuperAdmin returns to platform-level view

## Frontend Integration

### Impersonation Banner
When impersonating, display a banner at the top of the UI:

```tsx
{isImpersonating && (
  <div className="impersonation-banner">
    You're viewing as Tenant: {tenantName} 
    (Impersonation expires in {minutesRemaining} minutes)
    <button onClick={stopImpersonation}>Stop Impersonation</button>
  </div>
)}
```

### Example API Call

```typescript
// Start impersonation
const impersonate = async (tenantId: number) => {
  const response = await api.post(`/api/admin/tenants/${tenantId}/impersonate?durationMinutes=30`);
  
  // Store impersonation token
  localStorage.setItem('impersonationToken', response.data.impersonationToken);
  localStorage.setItem('impersonatedTenant', JSON.stringify(response.data.tenant));
  
  // Update API client to use impersonation token
  api.defaults.headers.common['Authorization'] = `Bearer ${response.data.impersonationToken}`;
  
  // Show banner
  showImpersonationBanner(response.data.banner);
};

// Stop impersonation
const stopImpersonation = async () => {
  await api.post('/api/admin/tenants/stop-impersonation');
  
  // Clear impersonation token
  localStorage.removeItem('impersonationToken');
  localStorage.removeItem('impersonatedTenant');
  
  // Restore SuperAdmin token
  api.defaults.headers.common['Authorization'] = `Bearer ${superAdminToken}`;
  
  // Hide banner
  hideImpersonationBanner();
};
```

## Security Considerations

1. **Impersonation Tokens**:
   - Short expiration (default: 30 minutes, max: 2 hours recommended)
   - Single-use tokens (optional, for extra security)
   - Clear indication in logs that action was via impersonation

2. **Audit Logging**:
   - All impersonation actions are logged with:
     - Original SuperAdmin identity
     - Impersonated tenant ID
     - Timestamp and duration

3. **Access Control**:
   - Only SuperAdmin can impersonate
   - Cannot impersonate suspended/deleted tenants
   - Impersonation token cannot be used for admin endpoints

4. **Data Isolation**:
   - During impersonation, SuperAdmin sees only tenant's data
   - Query filters apply normally for impersonated tenant
   - SuperAdmin permissions remain but within tenant context

## Usage Examples

### Example 1: Support Ticket
1. Customer reports issue with tenant "Acme Corp"
2. SuperAdmin finds tenant: `GET /api/admin/tenants?search=Acme`
3. SuperAdmin impersonates tenant: `POST /api/admin/tenants/1/impersonate`
4. SuperAdmin views tenant's data to diagnose issue
5. SuperAdmin stops impersonation after resolving issue

### Example 2: Plan Adjustment
1. Customer requests trial extension
2. SuperAdmin finds tenant: `GET /api/admin/tenants/1`
3. SuperAdmin extends trial: `POST /api/admin/subscriptions/5/extend-trial`
4. Action is logged in AdminAudit

### Example 3: Business Intelligence
1. SuperAdmin views metrics: `GET /api/admin/metrics/overview`
2. Identifies high churn plan
3. Analyzes churn: `GET /api/admin/metrics/churn-analysis`
4. Takes corrective action (e.g., improve plan features)

## Testing

Use the test script to verify impersonation:

```powershell
.\scripts\test-tenant-impersonation.ps1
```

This script:
1. Logs in as SuperAdmin
2. Lists tenants
3. Impersonates a tenant
4. Makes API calls as impersonated tenant
5. Stops impersonation
6. Verifies audit logs

