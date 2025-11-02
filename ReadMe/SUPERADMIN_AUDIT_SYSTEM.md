# SuperAdmin Audit System

## Overview

The SuperAdmin audit system logs all actions taken by SuperAdmin users for security, compliance, and accountability. SuperAdmin users operate at the **platform layer** with no `TenantId`, and their query filters are bypassed **only temporarily** for specific admin endpoints.

## Architecture

### 1. SuperAdmin Characteristics

- **Role**: `SuperAdmin` (JWT claim: `Role=SuperAdmin`)
- **TenantId**: `NULL` (no tenant association)
- **Query Filter Bypass**: Only on specific admin endpoints (not globally)
- **All Actions Logged**: Every SuperAdmin action is logged to `AdminAudit` table

### 2. Query Filter Bypass Mechanism

SuperAdmin query filters are **NOT bypassed globally**. Instead:

1. **Middleware Detection**: `SuperAdminQueryFilterBypassMiddleware` detects SuperAdmin requests
2. **Admin Endpoint Check**: Only admin endpoints trigger query filter bypass
3. **Temporary Bypass**: Sets `HttpContext.Items["BypassTenantQueryFilters"] = true` for the request
4. **ApplicationDbContext**: Checks this flag before bypassing query filters

**Admin Endpoints** (where query filters are bypassed):
- `/api/usermanagement/*`
- `/api/admin/*`
- `/api/tenants/*`
- `/api/subscriptions/plans/*`
- `/api/billing/webhooks/*`

### 3. Audit Logging

Every SuperAdmin action is automatically logged to `AdminAudit` table:

- **Who**: AdminUserId, AdminEmail
- **What**: ActionType, HttpMethod, Endpoint
- **When**: CreatedAt, DurationMs
- **Where**: IpAddress, UserAgent
- **Target**: TargetTenantId, TargetEntityType, TargetEntityId
- **Request**: RequestPayload (with sensitive data masked)
- **Response**: StatusCode, IsSuccess, ErrorMessage

## Implementation Details

### 1. AdminAudit Entity

```csharp
public class AdminAudit : BaseEntity
{
    public string AdminUserId { get; set; }
    public string AdminEmail { get; set; }
    public string ActionType { get; set; }  // e.g., "UserManagement.GetAll"
    public string HttpMethod { get; set; }
    public string Endpoint { get; set; }
    public string? TargetTenantId { get; set; }
    public string? TargetEntityType { get; set; }
    public string? TargetEntityId { get; set; }
    public string? RequestPayload { get; set; }
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Metadata { get; set; }
    public string? ErrorMessage { get; set; }
    public long? DurationMs { get; set; }
}
```

### 2. Middleware Pipeline

```
Authentication
  ↓
TenantResolutionMiddleware (sets tenant context)
  ↓
SuperAdminQueryFilterBypassMiddleware (temporarily enables query filter bypass)
  ↓
FeatureAccessMiddleware (checks subscription features)
  ↓
Authorization
  ↓
AdminAuditMiddleware (logs all SuperAdmin actions)
  ↓
Controllers
```

### 3. Query Filter Logic

**Before** (Global Bypass):
```csharp
entity.HasQueryFilter(e => IsSuperAdmin() || e.TenantId == _tenantProvider.TenantId);
```

**After** (Temporary Bypass):
```csharp
entity.HasQueryFilter(e => ShouldBypassTenantQueryFilters() || e.TenantId == _tenantProvider.TenantId);

private bool ShouldBypassTenantQueryFilters()
{
    if (!IsSuperAdmin()) return false;
    
    var httpContext = GetHttpContextAccessor()?.HttpContext;
    if (httpContext == null) return false;
    
    // Only bypass if explicitly enabled for this request (admin endpoints)
    return httpContext.Items.ContainsKey("BypassTenantQueryFilters") &&
           httpContext.Items["BypassTenantQueryFilters"] is bool bypass &&
           bypass;
}
```

### 4. AdminAuditMiddleware

The middleware:
1. **Detects SuperAdmin**: Checks if user has `Role=SuperAdmin`
2. **Captures Request**: HTTP method, endpoint, request payload, IP, user agent
3. **Executes Request**: Calls next middleware
4. **Captures Response**: Status code, success/failure, duration
5. **Logs Action**: Calls `IAdminAuditService.LogActionAsync()`

**Sensitive Data Masking**:
- Passwords: `"password":"***MASKED***"`
- Tokens: `"token":"***MASKED***"`
- Refresh Tokens: `"refreshToken":"***MASKED***"`

## API Endpoints

### Query Audit Logs

**GET** `/api/adminaudit`
- Query all audit logs with optional filters
- **Authorization**: `[Authorize(Roles = "SuperAdmin")]`

**Parameters**:
- `adminEmail` - Filter by admin email
- `actionType` - Filter by action type
- `targetTenantId` - Filter by target tenant
- `isSuccess` - Filter by success/failure
- `startDate` - Start date filter
- `endDate` - End date filter
- `pageNumber` - Page number (default: 1)
- `pageSize` - Page size (default: 50)

**Example**:
```bash
GET /api/adminaudit?adminEmail=superadmin@smallhr.com&actionType=UserManagement&pageNumber=1&pageSize=50
```

### Get Audit Log by ID

**GET** `/api/adminaudit/{id}`
- Get specific audit log entry
- **Authorization**: `[Authorize(Roles = "SuperAdmin")]`

### Get Statistics

**GET** `/api/adminaudit/statistics`
- Get statistics about admin actions
- **Authorization**: `[Authorize(Roles = "SuperAdmin")]`

**Parameters**:
- `startDate` - Start date filter
- `endDate` - End date filter

**Response**:
```json
{
  "totalActions": 150,
  "successfulActions": 145,
  "failedActions": 5,
  "successRate": 96.67,
  "averageDurationMs": 125,
  "topActionTypes": [
    { "actionType": "UserManagement.GetAll", "count": 50, ... }
  ],
  "topAdmins": [
    { "adminEmail": "superadmin@smallhr.com", "count": 150, ... }
  ]
}
```

## Security Considerations

### 1. Audit Log Security

- **Immutable Logs**: Audit logs should not be modified after creation
- **Access Control**: Only SuperAdmin can view audit logs
- **Data Retention**: Consider implementing retention policies
- **Encryption**: Sensitive data should be encrypted at rest

### 2. Query Filter Bypass

- **Temporary Only**: Query filters are bypassed only for the duration of the request
- **Endpoint Specific**: Only admin endpoints bypass filters
- **Audit Trail**: All actions are logged for accountability

### 3. Sensitive Data

- **Request Payload**: Masked for passwords, tokens, etc.
- **Truncation**: Payloads > 4000 chars are truncated
- **Error Messages**: Logged but should not expose sensitive information

## Usage Examples

### Example 1: SuperAdmin Accesses User Management

1. **Request**: `GET /api/usermanagement/users`
2. **Middleware**: `SuperAdminQueryFilterBypassMiddleware` detects admin endpoint
3. **Sets Flag**: `context.Items["BypassTenantQueryFilters"] = true`
4. **Query Filter**: Bypassed for this request only
5. **Audit Log**: Action logged to `AdminAudit` table

### Example 2: SuperAdmin Accesses Tenant Endpoint

1. **Request**: `GET /api/tenants/123`
2. **Middleware**: Admin endpoint detected
3. **Query Filter**: Bypassed (can access tenant 123 data)
4. **Audit Log**: Logged with `TargetTenantId = "123"`

### Example 3: SuperAdmin Accesses Regular Endpoint

1. **Request**: `GET /api/employees` (not an admin endpoint)
2. **Middleware**: No bypass flag set
3. **Query Filter**: **NOT bypassed** (follows tenant isolation)
4. **Result**: SuperAdmin sees only platform-level data or gets 403

## Testing

### Test Scenarios

1. **SuperAdmin on Admin Endpoint**: Query filters should be bypassed
2. **SuperAdmin on Regular Endpoint**: Query filters should NOT be bypassed
3. **Regular User on Admin Endpoint**: Query filters should NOT be bypassed
4. **Audit Logging**: All SuperAdmin actions should be logged
5. **Sensitive Data Masking**: Passwords/tokens should be masked in logs

## Migration

To add the AdminAudit table:

```bash
dotnet ef migrations add AddAdminAudit --project SmallHR.Infrastructure --startup-project SmallHR.API --context ApplicationDbContext
dotnet ef database update --project SmallHR.Infrastructure --startup-project SmallHR.API --context ApplicationDbContext
```

## Related Documentation

- [SuperAdmin Platform Layer Architecture](./SUPERADMIN_PLATFORM_LAYER.md)
- [SuperAdmin TenantId Fix](./SUPERADMIN_TENANTID_FIX.md)
- [Tenant Isolation Architecture](./TENANT_ISOLATION_ARCHITECTURE.md)

