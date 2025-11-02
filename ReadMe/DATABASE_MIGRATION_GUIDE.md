# Database Migration Guide

## Database Configuration

**Primary Database**: `SmallHRDb`  
**Server**: `(localdb)\mssqllocaldb`  
**Connection String**: `Server=(localdb)\\mssqllocaldb;Database=SmallHRDb;Trusted_Connection=true;MultipleActiveResultSets=true`

## Important Notes

⚠️ **All migrations should be applied to `SmallHRDb` only**

- Do NOT apply migrations to tenant-specific databases
- The system uses a **single database, shared schema** architecture
- All tenants share the same `SmallHRDb` database
- Tenant isolation is handled via `TenantId` column, not separate databases

## Running Migrations

### Apply All Pending Migrations

```bash
dotnet ef database update --project SmallHR.Infrastructure --startup-project SmallHR.API --context ApplicationDbContext
```

This will use the connection string from `appsettings.json` which points to `SmallHRDb`.

### Apply with Explicit Connection String

If you need to override the connection string:

```bash
dotnet ef database update --project SmallHR.Infrastructure --startup-project SmallHR.API --context ApplicationDbContext --connection "Server=(localdb)\mssqllocaldb;Database=SmallHRDb;Trusted_Connection=true;MultipleActiveResultSets=true"
```

### Create New Migration

```bash
dotnet ef migrations add MigrationName --project SmallHR.Infrastructure --startup-project SmallHR.API --context ApplicationDbContext
```

### List Applied Migrations

```bash
dotnet ef migrations list --project SmallHR.Infrastructure --startup-project SmallHR.API --context ApplicationDbContext
```

## Configuration Files

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SmallHRDb;Trusted_Connection=true;MultipleActiveResultSets=true",
    "Tenants": {}
  }
}
```

### DesignTimeDbContextFactory.cs

The design-time factory is configured to use `SmallHRDb`:

```csharp
var connectionString = "Server=(localdb)\\mssqllocaldb;Database=SmallHRDb;Trusted_Connection=true;MultipleActiveResultSets=true";
```

You can override it with the `EF_CONNECTION` environment variable if needed.

## Architecture

```
SmallHRDb (Single Database)
├── AspNetUsers (all users from all tenants)
├── AspNetRoles
├── Tenants (tenant registry)
├── Employees (with TenantId column)
├── LeaveRequests (with TenantId column)
├── Attendances (with TenantId column)
├── Departments (with TenantId column)
├── Positions (with TenantId column)
├── Subscriptions
├── SubscriptionPlans
├── TenantUsageMetrics
└── TenantLifecycleEvents
```

## Tenant Isolation

- **Row-Level Security (RLS)**: Implemented via EF Core query filters
- **TenantId Column**: All tenant-scoped entities have a `TenantId` column
- **SuperAdmin Bypass**: SuperAdmin users bypass tenant isolation

## Current Migration Status

All migrations have been applied to `SmallHRDb`:

✅ 20251028180828_InitialCreate
✅ 20251029180741_AddRolePermissions
✅ 20251029201018_ActionFlags_AddColumns
✅ 20251030182947_AddModules
✅ 20251030190320_AddTenantSupport
✅ 20251030190909_TenantId_AllAggregates
✅ 20251031201524_AddDepartmentsAndPositions
✅ 20251101123902_AddRoleToEmployee
✅ 20251101210006_AddSubscriptionFieldsToTenant
✅ 20251102070636_AddProvisioningFieldsToTenant
✅ 20251102091745_AddTenantUsageMetrics
✅ 20251102092344_AddTenantIdToUser
✅ 20251102093426_AddTenantLifecycleManagement
✅ 20251102095140_EnsureSuperAdminTenantIdNull
✅ 20251102095221_SetSuperAdminTenantIdToNull

## Troubleshooting

### Issue: Migrations applied to wrong database

**Solution**: 
1. Check `appsettings.json` connection string points to `SmallHRDb`
2. Use explicit connection string parameter when running migrations
3. Verify the database name before applying migrations

### Issue: Design-time factory uses wrong database

**Solution**: 
Set the `EF_CONNECTION` environment variable:
```bash
$env:EF_CONNECTION="Server=(localdb)\mssqllocaldb;Database=SmallHRDb;Trusted_Connection=true;MultipleActiveResultSets=true"
```

### Issue: Multiple databases exist

**Solution**: 
- Ensure you're working with `SmallHRDb` only
- Do NOT create tenant-specific databases
- All tenant data goes into `SmallHRDb` with `TenantId` isolation

