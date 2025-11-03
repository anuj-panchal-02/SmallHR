# Tenant ID Consistency Fix

## Problem

There was an inconsistency in how `TenantId` was stored in the database:

1. **When creating departments manually via API:**
   - Uses `_tenantProvider.TenantId` which returns the tenant ID (int) as a string
   - Example: `TenantId = "123"` (tenant ID as string)

2. **When seeding departments during tenant provisioning:**
   - Was using `tenant.Name?.ToLowerInvariant()` instead of `tenantId.ToString()`
   - Example: `TenantId = "acme-corp"` (tenant name as string)

This inconsistency caused:
- Departments created manually had `TenantId = "123"` (int as string)
- Departments seeded during tenant creation had `TenantId = "acme-corp"` (tenant name)
- Query filters and tenant isolation would fail because the formats didn't match

## Root Cause

In `TenantProvisioningService.cs` line 69:
```csharp
var tenantIdString = tenant.Name?.ToLowerInvariant() ?? tenantId.ToString();
```

This used the tenant name (e.g., "Acme Corp" → "acme-corp") instead of the tenant ID (e.g., "123") for seeding departments, positions, modules, and role permissions.

## Solution

Changed to use `tenantId.ToString()` consistently:

```csharp
// Use tenant ID (int) as string for consistency - not tenant name
// This ensures departments/positions seeded during provisioning use the same TenantId format
// as departments/positions created manually via API (which use _tenantProvider.TenantId)
var tenantIdString = tenantId.ToString();
```

## Files Modified

1. **`SmallHR.Infrastructure/Services/TenantProvisioningService.cs`**
   - Line 69-83: Changed `tenantIdString` to use `tenantId.ToString()` instead of `tenant.Name?.ToLowerInvariant()`
   - Added comments explaining the consistency requirement

## Impact

### ✅ **Fixed:**
- All entities (Departments, Positions, Modules, RolePermissions) seeded during tenant provisioning now use tenant ID (int as string)
- Consistency with entities created manually via API
- Query filters and tenant isolation now work correctly
- Tenant resolution middleware can properly filter data

### ✅ **Still Works:**
- Connection string resolver can still handle both tenant ID and tenant name (if needed for backward compatibility)
- Existing data is unaffected (this only affects new tenant provisioning)

## Database Schema

The `Department`, `Position`, `Module`, and `RolePermission` entities have:
```csharp
public required string TenantId { get; set; }
```

This field should always store the tenant ID (int) as a string, not the tenant name.

## Migration Notes

If you have existing data with tenant names in `TenantId` fields:
1. You may need to run a data migration to convert tenant names to tenant IDs
2. Query existing tenants and update their related entities
3. Example migration script:
```sql
UPDATE Departments 
SET TenantId = (SELECT CAST(Id AS NVARCHAR) FROM Tenants WHERE Name = Departments.TenantId)
WHERE TenantId NOT LIKE '%[^0-9]%' -- Only update if TenantId is not numeric
```

## Testing

After this fix:
1. Create a new tenant → Departments seeded should have `TenantId = "123"` (int as string)
2. Create a department manually → Should also have `TenantId = "123"` (int as string)
3. Query filters should work correctly for both seeded and manually created departments

