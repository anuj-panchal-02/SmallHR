# SuperAdmin Testing Summary

## Quick Start

### 1. Start the API

```powershell
cd SmallHR.API
dotnet run
```

The API will start on `http://localhost:5192` (or configured port).

### 2. Run Automated Test Script

```powershell
.\scripts\test-superadmin-actions.ps1
```

This script will:
- ✅ Login as SuperAdmin
- ✅ Test admin endpoints (UserManagement, Admin, Tenants)
- ✅ Verify query filters are bypassed
- ✅ Check audit logs are created
- ✅ Test regular endpoints (should NOT bypass filters)
- ✅ Get audit statistics

### 3. Manual Testing

#### Step 1: Login

```bash
POST http://localhost:5192/api/auth/login
Content-Type: application/json

{
  "email": "superadmin@smallhr.com",
  "password": "SuperAdmin@123"
}
```

**Save the token** from the response.

#### Step 2: Test Admin Endpoints (Query Filters Bypassed)

```bash
# User Management - should return all users
GET http://localhost:5192/api/usermanagement/users
Authorization: Bearer <token>

# Admin Verify - should return SuperAdmin status
GET http://localhost:5192/api/admin/verify-superadmin
Authorization: Bearer <token>

# Tenants - should return all tenants
GET http://localhost:5192/api/tenants
Authorization: Bearer <token>
```

**Expected**: All endpoints return data from all tenants (query filters bypassed).

#### Step 3: Check Audit Logs

```bash
# Get audit logs
GET http://localhost:5192/api/adminaudit?pageSize=10
Authorization: Bearer <token>

# Get audit statistics
GET http://localhost:5192/api/adminaudit/statistics
Authorization: Bearer <token>
```

**Expected**: All SuperAdmin actions are logged in AdminAudit table.

#### Step 4: Test Regular Endpoint (Query Filters NOT Bypassed)

```bash
# Employees - should respect tenant isolation
GET http://localhost:5192/api/employees
Authorization: Bearer <token>
```

**Expected**: Empty or platform-only data (query filters NOT bypassed).

## What to Verify

### ✅ Query Filter Bypass

- [ ] Admin endpoints return data from all tenants
- [ ] Regular endpoints return only platform/tenant-specific data
- [ ] `HttpContext.Items["BypassTenantQueryFilters"]` is set only on admin endpoints

### ✅ Audit Logging

- [ ] All SuperAdmin actions logged to AdminAudit table
- [ ] Request payloads captured (with sensitive data masked)
- [ ] IP address and user agent captured
- [ ] Duration tracked
- [ ] Success/failure status recorded

### ✅ Security

- [ ] Only SuperAdmin can access admin endpoints
- [ ] Regular users get 403 on admin endpoints
- [ ] Query filters bypassed only temporarily on admin endpoints
- [ ] Regular endpoints still respect tenant isolation

## Expected Database Queries

### Verify AdminAudit Logs

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

**Expected**: `TenantId = NULL` for all SuperAdmin users.

## Test Files

- **PowerShell Script**: `scripts/test-superadmin-actions.ps1`
- **REST Client File**: `scripts/test-superadmin-api.http`
- **Full Guide**: `ReadMe/SUPERADMIN_TESTING_GUIDE.md`

## Troubleshooting

### Issue: 401 Unauthorized

**Solution**: 
1. Verify SuperAdmin login credentials
2. Check JWT token is valid
3. Ensure token is included in Authorization header

### Issue: 403 Forbidden on Admin Endpoints

**Solution**:
1. Verify user has `Role=SuperAdmin` in JWT token
2. Check authorization policies
3. Verify middleware order

### Issue: No Audit Logs Created

**Solution**:
1. Verify `AdminAuditMiddleware` is registered after authorization
2. Check `IAdminAuditService` is registered in DI
3. Verify AdminAudit table exists
4. Check application logs for errors

### Issue: Query Filters Still Applied on Admin Endpoints

**Solution**:
1. Verify `SuperAdminQueryFilterBypassMiddleware` is registered
2. Check endpoint path matches admin endpoint patterns
3. Verify `HttpContext.Items["BypassTenantQueryFilters"]` is set
4. Check `ShouldBypassTenantQueryFilters()` in ApplicationDbContext

