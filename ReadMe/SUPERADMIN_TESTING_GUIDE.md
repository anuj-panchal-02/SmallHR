# SuperAdmin Testing Guide

## Overview

This guide helps you test SuperAdmin actions on admin endpoints and verify that:
1. SuperAdmin can access admin endpoints
2. Query filters are bypassed only on admin endpoints
3. All SuperAdmin actions are logged to AdminAudit table
4. Regular endpoints still respect tenant isolation

## Prerequisites

1. **SuperAdmin User**: Ensure SuperAdmin user exists with `TenantId = NULL`
   - Email: `superadmin@smallhr.com`
   - Password: `SuperAdmin@123`
   - Role: `SuperAdmin`
   - TenantId: `NULL`

2. **Database**: Migrations applied to `SmallHRDb`
   - AdminAudit table created
   - All migrations applied

3. **Application**: API running on `http://localhost:5000` (or configured port)

## Testing Steps

### Step 1: Login as SuperAdmin

```bash
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "superadmin@smallhr.com",
  "password": "SuperAdmin@123"
}
```

**Expected Response**:
```json
{
  "token": "...",
  "refreshToken": "...",
  "expiration": "...",
  "user": {
    "email": "superadmin@smallhr.com",
    "roles": ["SuperAdmin"],
    "tenantId": null
  }
}
```

**Verify**:
- ✅ Token received
- ✅ User has `Role=SuperAdmin`
- ✅ User has `TenantId=null` (or no TenantId claim)

### Step 2: Test Admin Endpoints (Query Filters Bypassed)

#### 2.1 User Management Endpoint

```bash
GET http://localhost:5000/api/usermanagement/users
Authorization: Bearer <token>
```

**Expected**:
- ✅ Returns all users from all tenants
- ✅ Query filters are bypassed
- ✅ Action logged in AdminAudit

#### 2.2 Admin Controller - Verify SuperAdmin

```bash
GET http://localhost:5000/api/admin/verify-superadmin
Authorization: Bearer <token>
```

**Expected Response**:
```json
{
  "isValid": true,
  "superAdminRoleExists": true,
  "totalSuperAdmins": 1,
  "correctConfiguration": 1,
  "needsFix": 0,
  "issues": []
}
```

#### 2.3 Tenants Endpoint

```bash
GET http://localhost:5000/api/tenants
Authorization: Bearer <token>
```

**Expected**:
- ✅ Returns all tenants
- ✅ Query filters bypassed
- ✅ Action logged in AdminAudit

### Step 3: Test Regular Endpoints (Query Filters NOT Bypassed)

#### 3.1 Employees Endpoint

```bash
GET http://localhost:5000/api/employees
Authorization: Bearer <token>
```

**Expected**:
- ✅ May return empty list (no employees for platform tenant)
- ✅ Query filters NOT bypassed (SuperAdmin sees only platform-level data)
- ✅ Action logged in AdminAudit

### Step 4: Verify Audit Logs

#### 4.1 Get Audit Logs

```bash
GET http://localhost:5000/api/adminaudit?pageSize=10
Authorization: Bearer <token>
```

**Expected Response**:
```json
{
  "totalCount": 5,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1,
  "auditLogs": [
    {
      "id": 1,
      "adminUserId": "...",
      "adminEmail": "superadmin@smallhr.com",
      "actionType": "UserManagement.GetAll",
      "httpMethod": "GET",
      "endpoint": "/api/usermanagement/users",
      "targetTenantId": null,
      "statusCode": 200,
      "isSuccess": true,
      "durationMs": 125,
      "createdAt": "..."
    }
  ]
}
```

**Verify**:
- ✅ All SuperAdmin actions are logged
- ✅ Request payload is masked (passwords/tokens)
- ✅ IP address and user agent captured
- ✅ Duration tracked

#### 4.2 Get Audit Statistics

```bash
GET http://localhost:5000/api/adminaudit/statistics
Authorization: Bearer <token>
```

**Expected Response**:
```json
{
  "totalActions": 10,
  "successfulActions": 9,
  "failedActions": 1,
  "successRate": 90.0,
  "averageDurationMs": 125,
  "topActionTypes": [
    {
      "actionType": "UserManagement.GetAll",
      "count": 5,
      "successCount": 5,
      "failureCount": 0
    }
  ],
  "topAdmins": [
    {
      "adminEmail": "superadmin@smallhr.com",
      "count": 10,
      "successCount": 9,
      "failureCount": 1
    }
  ]
}
```

## Using Test Scripts

### PowerShell Script

Run the automated test script:

```powershell
.\scripts\test-superadmin-actions.ps1
```

Or with custom parameters:

```powershell
.\scripts\test-superadmin-actions.ps1 -BaseUrl "http://localhost:5000" -SuperAdminEmail "superadmin@smallhr.com" -SuperAdminPassword "SuperAdmin@123"
```

### REST Client (VS Code)

1. Install REST Client extension in VS Code
2. Open `scripts/test-superadmin-api.http`
3. Execute requests sequentially
4. Copy token from login response to `@token` variable

## Verification Checklist

### ✅ Query Filter Bypass

- [ ] SuperAdmin can access `/api/usermanagement/users` (returns all users)
- [ ] SuperAdmin can access `/api/tenants` (returns all tenants)
- [ ] SuperAdmin can access `/api/admin/*` endpoints
- [ ] SuperAdmin on `/api/employees` returns empty or platform-only data

### ✅ Audit Logging

- [ ] All SuperAdmin actions logged to AdminAudit table
- [ ] Request payload masked (passwords/tokens)
- [ ] IP address and user agent captured
- [ ] Duration tracked
- [ ] Success/failure status recorded

### ✅ Security

- [ ] Only SuperAdmin can access admin endpoints
- [ ] Regular users cannot access admin endpoints (403)
- [ ] Query filters bypassed only on admin endpoints
- [ ] Regular endpoints still respect tenant isolation

## Troubleshooting

### Issue: SuperAdmin cannot access admin endpoints

**Solution**:
1. Verify SuperAdmin has `Role=SuperAdmin` in JWT token
2. Check `SuperAdminQueryFilterBypassMiddleware` is registered
3. Verify endpoint path matches admin endpoint patterns
4. Check middleware order in `Program.cs`

### Issue: Audit logs not being created

**Solution**:
1. Verify `AdminAuditMiddleware` is registered after authorization
2. Check `IAdminAuditService` is registered in DI
3. Verify AdminAudit table exists in database
4. Check application logs for errors

### Issue: Query filters still applied on admin endpoints

**Solution**:
1. Verify `SuperAdminQueryFilterBypassMiddleware` sets `HttpContext.Items["BypassTenantQueryFilters"]`
2. Check `ShouldBypassTenantQueryFilters()` in ApplicationDbContext
3. Verify admin endpoint paths match the middleware patterns
4. Check middleware order (must be before FeatureAccessMiddleware)

### Issue: Regular endpoints bypass query filters

**Solution**:
1. Verify endpoint is NOT in admin endpoints list
2. Check `ShouldBypassTenantQueryFilters()` returns false for non-admin endpoints
3. Verify `HttpContext.Items["BypassTenantQueryFilters"]` is not set

## Expected Behavior

### Admin Endpoints (Query Filters Bypassed)

| Endpoint | Expected Behavior |
|----------|-------------------|
| `/api/usermanagement/*` | Returns all users from all tenants |
| `/api/admin/*` | Returns all data (platform-level) |
| `/api/tenants` | Returns all tenants |
| `/api/subscriptions/plans` | Returns all subscription plans |

### Regular Endpoints (Query Filters Applied)

| Endpoint | Expected Behavior |
|----------|-------------------|
| `/api/employees` | Returns empty or platform-only data |
| `/api/leaverequests` | Returns empty or platform-only data |
| `/api/attendances` | Returns empty or platform-only data |
| `/api/departments` | Returns empty or platform-only data |

## Database Verification

### Check AdminAudit Logs Directly

```sql
SELECT TOP 10
    Id,
    AdminEmail,
    ActionType,
    HttpMethod,
    Endpoint,
    StatusCode,
    IsSuccess,
    DurationMs,
    CreatedAt
FROM AdminAudits
ORDER BY CreatedAt DESC;
```

### Verify SuperAdmin User

```sql
SELECT 
    u.Id,
    u.Email,
    u.TenantId,
    r.Name AS Role
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'SuperAdmin';
```

**Expected**:
- `TenantId` should be `NULL` for all SuperAdmin users

## Related Documentation

- [SuperAdmin Audit System](./SUPERADMIN_AUDIT_SYSTEM.md)
- [SuperAdmin Platform Layer](./SUPERADMIN_PLATFORM_LAYER.md)
- [SuperAdmin TenantId Fix](./SUPERADMIN_TENANTID_FIX.md)

