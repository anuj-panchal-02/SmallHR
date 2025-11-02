# SuperAdmin TenantId Fix Guide

## Overview

SuperAdmin users operate at the **platform layer** and must have `TenantId = NULL`. This document describes how to ensure all SuperAdmin users have the correct configuration.

## Why SuperAdmin Must Have TenantId = NULL

- SuperAdmin operates at the **platform layer**, not the tenant layer
- SuperAdmin bypasses tenant isolation to manage all tenants
- SuperAdmin should NOT be associated with any specific tenant
- Row-Level Security (RLS) query filters are bypassed for SuperAdmin

## Automatic Fixes

### 1. Application Startup
The application automatically fixes SuperAdmin users during startup:
- Checks all users with SuperAdmin role
- Sets `TenantId = NULL` for any SuperAdmin user that has a TenantId set
- Logs the updates

### 2. User Creation
When creating a new SuperAdmin user via API:
- `TenantId` is set to `NULL` automatically
- Validation ensures SuperAdmin users never get a TenantId

### 3. Seed Data
During initial data seeding:
- SuperAdmin user is created with `TenantId = NULL`
- Existing SuperAdmin users are updated if they have a TenantId

## Manual Fixes

### Option 1: SQL Script

Run the SQL script to update all SuperAdmin users:

```bash
sqlcmd -S localhost -d SmallHR -i scripts/fix-superadmin-tenantid.sql
```

Or manually execute:
```sql
-- Update all users with SuperAdmin role to have TenantId = NULL
UPDATE [AspNetUsers]
SET [TenantId] = NULL
WHERE [Id] IN (
    SELECT ur.[UserId]
    FROM [AspNetUserRoles] ur
    INNER JOIN [AspNetRoles] r ON ur.[RoleId] = r.[Id]
    WHERE r.[Name] = 'SuperAdmin'
)
AND [TenantId] IS NOT NULL;
```

### Option 2: PowerShell Script

Run the PowerShell script:

```powershell
.\scripts\fix-superadmin-tenantid.ps1
```

You can also specify a custom connection string:

```powershell
.\scripts\fix-superadmin-tenantid.ps1 -ConnectionString "Server=localhost;Database=SmallHR;Integrated Security=true;TrustServerCertificate=true;"
```

### Option 3: API Endpoint

Use the Admin API endpoint (requires SuperAdmin authentication):

```bash
# Verify current status
GET /api/admin/verify-superadmin

# Get all SuperAdmin users
GET /api/admin/superadmin-users

# Fix all SuperAdmin users
POST /api/admin/fix-superadmin-tenantid
```

Example using curl:
```bash
# Get auth token first
TOKEN=$(curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"superadmin@smallhr.com","password":"SuperAdmin@123"}' \
  | jq -r '.token')

# Verify SuperAdmin status
curl -X GET http://localhost:5000/api/admin/verify-superadmin \
  -H "Authorization: Bearer $TOKEN"

# Fix SuperAdmin users
curl -X POST http://localhost:5000/api/admin/fix-superadmin-tenantid \
  -H "Authorization: Bearer $TOKEN"
```

## Verification

### 1. Check SuperAdmin Users

```sql
SELECT 
    u.[Email],
    u.[FirstName],
    u.[LastName],
    u.[TenantId],
    r.[Name] AS [Role]
FROM [AspNetUsers] u
INNER JOIN [AspNetUserRoles] ur ON u.[Id] = ur.[UserId]
INNER JOIN [AspNetRoles] r ON ur.[RoleId] = r.[Id]
WHERE r.[Name] = 'SuperAdmin';
```

Expected result:
- All SuperAdmin users should have `TenantId = NULL`
- No SuperAdmin user should have a non-null TenantId

### 2. Verify via API

```bash
GET /api/admin/verify-superadmin
```

Expected response:
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

### 3. Check Application Logs

During startup, you should see:
```
Updated SuperAdmin user superadmin@smallhr.com to have TenantId = null
```

## Troubleshooting

### Issue: SuperAdmin still has TenantId after fix

**Solution**: 
1. Check if the user actually has the SuperAdmin role
2. Ensure the fix script/API endpoint was executed successfully
3. Verify the database connection string is correct

### Issue: New SuperAdmin users get TenantId assigned

**Solution**:
1. Check `UserManagementController.Create` - it should set `TenantId = null` for SuperAdmin
2. Ensure `ApplicationDbContext.ApplyTenantId()` bypasses SuperAdmin users
3. Verify seed data logic sets `TenantId = null` for SuperAdmin

### Issue: API endpoint returns 401 Unauthorized

**Solution**:
1. Ensure you're authenticated as a SuperAdmin user
2. Check that the JWT token includes the SuperAdmin role
3. Verify the authorization policy allows SuperAdmin

## Prevention

To prevent SuperAdmin users from getting TenantId assigned:

1. **Always use the API** to create SuperAdmin users (don't create manually in DB)
2. **Check user creation logic** - ensure SuperAdmin gets `TenantId = null`
3. **Run verification** after creating new SuperAdmin users
4. **Monitor application logs** for SuperAdmin TenantId updates

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/verify-superadmin` | Verify SuperAdmin configuration |
| GET | `/api/admin/superadmin-users` | Get all SuperAdmin users and status |
| POST | `/api/admin/fix-superadmin-tenantid` | Fix all SuperAdmin users |

All endpoints require `[Authorize(Roles = "SuperAdmin")]` authorization.

## Related Documentation

- [SuperAdmin Platform Layer Architecture](./SUPERADMIN_PLATFORM_LAYER.md)
- [Tenant Isolation Architecture](./TENANT_ISOLATION_ARCHITECTURE.md)

