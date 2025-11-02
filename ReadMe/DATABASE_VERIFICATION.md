# Database Verification

## Current Configuration

**Primary Database**: `SmallHRDb`  
**Server**: `(localdb)\mssqllocaldb`  
**Connection String**: `Server=(localdb)\\mssqllocaldb;Database=SmallHRDb;Trusted_Connection=true;MultipleActiveResultSets=true`

## Migration Status

### SmallHRDb (CORRECT - Primary Database)

✅ **All 16 migrations applied**:
- 20251028180828_InitialCreate
- 20251029180741_AddRolePermissions
- 20251029201018_ActionFlags_AddColumns
- 20251030182947_AddModules
- 20251030190320_AddTenantSupport
- 20251030190909_TenantId_AllAggregates
- 20251031201524_AddDepartmentsAndPositions
- 20251101123902_AddRoleToEmployee
- 20251101210006_AddSubscriptionFieldsToTenant
- 20251102070636_AddProvisioningFieldsToTenant
- 20251102091745_AddTenantUsageMetrics
- 20251102092344_AddTenantIdToUser
- 20251102093426_AddTenantLifecycleManagement
- 20251102095140_EnsureSuperAdminTenantIdNull
- 20251102095221_SetSuperAdminTenantIdToNull
- 20251102100418_AddAdminAudit

✅ **Key Tables Verified**:
- AdminAudits ✓
- __EFMigrationsHistory ✓
- AspNetUsers ✓
- Tenants ✓
- Employees ✓
- LeaveRequests ✓
- Attendances ✓
- And 16 more tables...

**Total Tables**: 23 tables

### Note on Other Databases

⚠️ **Note**: If you see other databases besides `SmallHRDb`, they should NOT be used.
- All migrations should be on `SmallHRDb` only
- Other databases can be ignored or deleted if not needed

## Verification Script

Run the verification script to check both databases:

```powershell
.\scripts\verify-database.ps1
```

## Ensuring Correct Database Usage

### 1. appsettings.json

✅ Already configured correctly:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SmallHRDb;..."
  }
}
```

### 2. DesignTimeDbContextFactory.cs

✅ Already configured correctly:
```csharp
var connectionString = "Server=(localdb)\\mssqllocaldb;Database=SmallHRDb;...";
```

### 3. Running Migrations

**Always use explicit connection string** to ensure migrations go to SmallHRDb:

```bash
dotnet ef database update --project SmallHR.Infrastructure --startup-project SmallHR.API --context ApplicationDbContext --connection "Server=(localdb)\mssqllocaldb;Database=SmallHRDb;Trusted_Connection=true;MultipleActiveResultSets=true"
```

**Or use the connection string from appsettings.json** (which points to SmallHRDb):

```bash
dotnet ef database update --project SmallHR.Infrastructure --startup-project SmallHR.API --context ApplicationDbContext
```

## Current Status

✅ **SmallHRDb**: All migrations applied correctly  
✅ **Configuration**: All files point to SmallHRDb  
✅ **AdminAudits table**: Created and ready  

**From now on, all migrations will be applied to SmallHRDb only.**

## Cleanup (Optional)

If you want to clean up other databases, you can drop them:

```sql
-- Example: Drop a tenant-specific database if it exists
DROP DATABASE IF EXISTS [DatabaseName];
```

**Note**: Only do this if you're sure you don't need any data from the database. All migrations and data should be in `SmallHRDb` only.

