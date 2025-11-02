# Tenant Isolation Architecture

## Overview

SmallHR uses **Single Database, Shared Schema (Soft Isolation)** for multi-tenancy. All tenants share the same database tables, and each record includes a `TenantId` column to ensure data isolation.

---

## Architecture: Single Database, Shared Schema

### Description

All tenants share the same database and schema. Each table that requires tenant isolation includes a `TenantId` column. Data isolation is enforced at the application layer through:

- **Global Query Filters**: Entity Framework Core automatically filters all queries by `TenantId`
- **Automatic Tenant Assignment**: New entities automatically receive the current tenant's ID
- **Security Enforcement**: Attempts to modify or access cross-tenant data are prevented

### Pros ✅

- **Cheapest to maintain**: Single database, simple backups, one schema to manage
- **Easy to scale horizontally**: Can add read replicas, sharding, or migrate to database-per-tenant when needed
- **Simple schema updates**: One migration applies to all tenants
- **Cost-effective**: Lower infrastructure costs, especially in early stages
- **Simplified operations**: One database to monitor, backup, and maintain

### Cons ⚠️

- **Harder to ensure strict data isolation**: Requires careful application-level enforcement
- **Complex queries must always filter by TenantId**: Global filters help, but raw SQL needs attention
- **Performance considerations**: As tenant count grows, single database may become a bottleneck
- **Custom reports require tenant filtering**: All queries must respect tenant boundaries
- **Data recovery**: More complex if you need to restore a single tenant's data

### Used By

This approach is used by:
- **Slack** (in early stages)
- **Trello** (initially)
- Many SaaS startups in MVP stage
- Most multi-tenant applications with < 1000 tenants

### Best For

- ✅ Early-stage SaaS with many small customers
- ✅ Applications with similar tenant sizes
- ✅ When cost optimization is critical
- ✅ When most queries are tenant-scoped
- ✅ Teams with limited database administration resources

---

## Current Implementation in SmallHR

### 1. Entity Design

All tenant-scoped entities inherit from `BaseEntity` and include a `TenantId` property:

```csharp
public class Employee : BaseEntity
{
    public required string TenantId { get; set; }
    // ... other properties
}
```

**Entities with TenantId:**
- `Employee`
- `LeaveRequest`
- `Attendance`
- `Department`
- `Position`
- `Module`
- `RolePermission`

**Entities without TenantId:**
- `Tenant` (tenant configuration itself)
- `User` (managed separately, though can be tenant-scoped if needed)

### 2. Global Query Filters

Entity Framework Core automatically filters all queries using `HasQueryFilter`:

```csharp
// ApplicationDbContext.cs
builder.Entity<Employee>(entity =>
{
    entity.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
});

builder.Entity<LeaveRequest>(entity =>
{
    entity.HasQueryFilter(lr => lr.TenantId == _tenantProvider.TenantId);
});

// ... similar for all tenant-scoped entities
```

**Benefits:**
- ✅ Developers can't accidentally forget tenant filtering
- ✅ Automatic isolation in LINQ queries
- ✅ Works with `.Include()`, `.ThenInclude()`, and complex queries

**Note:** Global filters can be disabled using `.IgnoreQueryFilters()` when needed (e.g., for SuperAdmin operations).

### 3. Automatic Tenant Assignment

The `ApplicationDbContext` automatically assigns `TenantId` to new entities in `SaveChanges`:

```csharp
private void ApplyTenantId()
{
    var currentTenantId = _tenantProvider.TenantId;
    foreach (var entry in ChangeTracker.Entries())
    {
        var tenantProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId");
        if (tenantProp != null && entry.State == EntityState.Added)
        {
            tenantProp.CurrentValue = currentTenantId;
        }
    }
}
```

**Security Features:**
- Prevents modification of `TenantId` after creation
- Blocks cross-tenant updates and deletes
- Throws `UnauthorizedAccessException` on violations

### 4. Tenant Resolution

Tenants are resolved in the `TenantResolutionMiddleware` with support for multiple detection methods:

**Resolution Priority (Highest to Lowest):**

1. **JWT Claims** (`TenantId` or `tenant` claim) - When user is authenticated
   - Most authoritative source
   - Automatically extracted from JWT token
   - Used for all authenticated requests

2. **Subdomain Detection** (`tenantname.yourapp.com`)
   - Extracts tenant from request hostname
   - Works for both authenticated and unauthenticated requests
   - Perfect for login flows where tenant context is needed before authentication
   - Examples:
     - `acme.yourapp.com` → `acme`
     - `company.localhost` → `company`

3. **X-Tenant-Id Header**
   - Direct tenant ID in request header
   - Useful for API clients and testing

4. **X-Tenant-Domain Header**
   - Tenant domain name in request header
   - Useful for domain-based tenant lookup

5. **Default Fallback**
   - Falls back to `"default"` if no tenant can be resolved

**Security Enforcement:**

If a user is authenticated, the JWT tenant claim **must** match the resolved tenant ID. This prevents cross-tenant access attempts:

```csharp
// If JWT has tenant claim, it must match resolved tenant
if (jwtTenant != null && jwtTenant != resolvedTenantId)
{
    return 403 Forbidden; // Tenant mismatch
}
```

**Implementation:**

```csharp
public class TenantResolutionMiddleware
{
    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        string? resolvedTenantId = null;
        var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;

        // Priority 1: JWT Claims (if authenticated)
        if (isAuthenticated)
        {
            var jwtTenant = context.User.FindFirst("TenantId")?.Value 
                ?? context.User.FindFirst("tenant")?.Value;
            if (!string.IsNullOrWhiteSpace(jwtTenant))
            {
                resolvedTenantId = jwtTenant;
            }
        }

        // Priority 2: Subdomain detection
        if (string.IsNullOrWhiteSpace(resolvedTenantId))
        {
            var subdomain = ExtractSubdomain(context.Request.Host.Host);
            if (!string.IsNullOrWhiteSpace(subdomain))
            {
                resolvedTenantId = subdomain.ToLowerInvariant();
            }
        }

        // Priority 3-5: Headers and default fallback...
        
        // Enforce tenant boundary
        if (isAuthenticated && jwtTenant != null && jwtTenant != resolvedTenantId)
        {
            return 403 Forbidden;
        }

        context.Items["TenantId"] = resolvedTenantId;
        await _next(context);
    }
}
```

**Subdomain Extraction:**

- Validates subdomain format (alphanumeric and hyphens)
- Excludes common non-tenant subdomains (`www`, `api`, `app`, `admin`)
- Handles `localhost` for development (returns null)
- Examples:
  - `acme.yourapp.com` → `acme`
  - `my-company.localhost` → `my-company`
  - `yourapp.com` → `null` (no subdomain)

### 5. Database Indexes

All tenant-scoped entities have indexes on `TenantId` for performance:

```csharp
entity.HasIndex(e => e.TenantId);
entity.HasIndex(e => new { e.TenantId, e.EmployeeId }).IsUnique();
```

**Why Indexes Matter:**
- Queries filtered by `TenantId` are fast
- Composite indexes support tenant-scoped uniqueness
- Prevents performance degradation as data grows

---

## Security Considerations

### ✅ Current Protections

1. **Global Query Filters**: Prevent cross-tenant reads
2. **Automatic Tenant Assignment**: Prevents wrong-tenant writes
3. **SaveChanges Validation**: Blocks `TenantId` modification
4. **JWT Claim Validation**: Middleware enforces tenant boundary
5. **Database Indexes**: Unique constraints are tenant-scoped

### ⚠️ Potential Risks

1. **Raw SQL Queries**: Must manually include `WHERE TenantId = @tenantId`
   - **Mitigation**: Use EF Core LINQ when possible, review all raw SQL

2. **Stored Procedures**: Must include tenant filtering
   - **Mitigation**: Pass `TenantId` as parameter

3. **Database-Level Access**: Direct database access can bypass filters
   - **Mitigation**: Restrict database access, use application users only

4. **Backup/Restore**: Restoring a single tenant requires careful extraction
   - **Mitigation**: Implement tenant-specific backup scripts

---

## Best Practices

### 1. Always Use ITenantProvider

```csharp
// ✅ Good
public class EmployeeService
{
    private readonly ITenantProvider _tenantProvider;
    
    public EmployeeService(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }
    
    public async Task<Employee> GetEmployeeAsync(int id)
    {
        // EF Core automatically filters by TenantId via global filter
        return await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
    }
}
```

### 2. Raw SQL Queries Must Include TenantId

```csharp
// ⚠️ Bad - Missing tenant filter
var sql = "SELECT * FROM Employees WHERE Department = @department";

// ✅ Good - Includes tenant filter
var sql = @"SELECT * FROM Employees 
            WHERE Department = @department 
            AND TenantId = @tenantId";
var parameters = new[] 
{ 
    new SqlParameter("@department", department),
    new SqlParameter("@tenantId", _tenantProvider.TenantId)
};
```

### 3. SuperAdmin Operations

For operations that need to access all tenants (SuperAdmin only):

```csharp
// Use IgnoreQueryFilters() carefully and only for SuperAdmin
var allTenantsEmployees = await _context.Employees
    .IgnoreQueryFilters()
    .Where(e => e.IsActive)
    .ToListAsync();
```

### 4. Testing Tenant Isolation

Always test that tenants cannot access each other's data:

```csharp
[Test]
public async Task GetEmployee_ShouldOnlyReturnTenantData()
{
    // Arrange
    var tenant1Employee = new Employee { TenantId = "tenant1", ... };
    var tenant2Employee = new Employee { TenantId = "tenant2", ... };
    
    // Act
    var result = await _service.GetEmployeeAsync(tenant1Employee.Id);
    
    // Assert
    Assert.That(result.TenantId, Is.EqualTo("tenant1"));
    Assert.That(result.Id, Is.Not.EqualTo(tenant2Employee.Id));
}
```

---

## Performance Optimization

### 1. Composite Indexes

Use composite indexes for common query patterns:

```csharp
// If you often query by TenantId + Department
entity.HasIndex(e => new { e.TenantId, e.Department });

// If you often query by TenantId + Status
entity.HasIndex(lr => new { lr.TenantId, lr.Status });
```

### 2. Partitioning (Future)

For very large datasets, consider table partitioning by `TenantId`:

```sql
-- Example SQL Server partitioning
CREATE PARTITION FUNCTION TenantPF (NVARCHAR(64))
AS RANGE RIGHT FOR VALUES ('tenant100', 'tenant200', 'tenant300');
```

### 3. Read Replicas

As you scale, use read replicas for reporting queries:

```csharp
// Future: Route read queries to replica
var readContext = _connectionResolver.GetReadOnlyContext(_tenantProvider.TenantId);
var employees = await readContext.Employees.ToListAsync();
```

---

## Migration Paths

### When to Consider Database-Per-Tenant

Consider migrating to **database-per-tenant** when:

1. **Tenant Count**: > 1000 tenants with active usage
2. **Data Volume**: Individual tenants have > 100GB of data
3. **Compliance**: Regulatory requirements demand physical isolation
4. **Customization**: Tenants need schema-level customizations
5. **Performance**: Single database becomes a bottleneck

### Architecture Supports Future Migration

SmallHR is designed to support database-per-tenant migration:

1. **IConnectionResolver**: Already supports per-tenant connection strings
2. **Tenant Resolution**: Works with any isolation model
3. **Query Filters**: Can be disabled for dedicated databases

**Migration Process:**
1. Create dedicated database for tenant
2. Apply migrations to new database
3. Migrate data from shared to dedicated database
4. Update `ConnectionStrings:Tenants:{tenantId}` in configuration
5. Restart application or refresh configuration

See **[MULTI_TENANCY_OPERATIONS.md](./MULTI_TENANCY_OPERATIONS.md)** for detailed migration steps.

---

## Alternative Isolation Models

### B. Database-Per-Tenant (Hard Isolation)

Each tenant has their own database.

**Pros:**
- ✅ Strongest data isolation
- ✅ Per-tenant backups and restores
- ✅ Schema customization per tenant
- ✅ Better performance for large tenants

**Cons:**
- ❌ Higher operational complexity
- ❌ More expensive (more databases)
- ❌ Schema updates must run on all databases
- ❌ Connection pooling challenges

**Best For:**
- Enterprise SaaS with large tenants
- Compliance requirements (HIPAA, GDPR, SOC 2)
- Tenants with vastly different needs

### C. Schema-Per-Tenant

Each tenant has their own schema within the same database.

**Pros:**
- ✅ Moderate isolation
- ✅ Easier than database-per-tenant
- ✅ Can still share connections

**Cons:**
- ❌ Schema management complexity
- ❌ Limited database support (PostgreSQL, Oracle)
- ❌ Migration complexity

**Best For:**
- Medium-scale SaaS
- PostgreSQL deployments
- When database-per-tenant is overkill

---

## Monitoring & Observability

### Key Metrics to Track

1. **Cross-Tenant Access Attempts**: 403 responses with "Tenant mismatch"
2. **Query Performance by Tenant**: Monitor slow queries per tenant
3. **Database Size Growth**: Track per-tenant data growth
4. **Connection Pool Usage**: Monitor if single database is bottlenecking

### Logging

Always include `TenantId` in structured logs:

```csharp
_logger.LogInformation("Employee created. TenantId: {TenantId}, EmployeeId: {EmployeeId}", 
    _tenantProvider.TenantId, employee.Id);
```

---

## Summary

SmallHR's **Single Database, Shared Schema** approach provides:

✅ **Cost-effective** multi-tenancy for early-stage SaaS  
✅ **Simple operations** with one database to manage  
✅ **Strong isolation** through application-level enforcement  
✅ **Easy scaling** path to database-per-tenant when needed  

The implementation ensures data isolation through:
- Global query filters
- Automatic tenant assignment
- Security validations in SaveChanges
- JWT claim enforcement
- Comprehensive database indexes

For migration planning, see **[MULTI_TENANCY_OPERATIONS.md](./MULTI_TENANCY_OPERATIONS.md)**.


