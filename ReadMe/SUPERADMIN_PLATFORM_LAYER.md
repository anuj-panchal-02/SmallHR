# SuperAdmin Platform Layer Architecture

## Overview

SuperAdmin is the **platform-level account** that operates **outside the tenant boundary**. It manages tenants, plans, users, and the system itself, belonging to the **Platform Layer** rather than the **Tenant Layer**.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│              Platform Layer (SuperAdmin)                │
│  - No TenantId association                              │
│  - Access to all tenants' data via controlled APIs     │
│  - Manages tenants, subscriptions, plans               │
└─────────────────────────────────────────────────────────┘
                            │
                            │
┌─────────────────────────────────────────────────────────┐
│              Tenant Layer (Regular Users)                │
│  - Each user has TenantId                               │
│  - Row-Level Security (RLS) applied                     │
│  - Tenant isolation enforced                            │
└─────────────────────────────────────────────────────────┘
```

## Key Characteristics

### 1. No TenantId Association
- SuperAdmin users **do NOT have a TenantId** (null)
- They are platform operators, not tenant members

### 2. Bypass Tenant Isolation
- SuperAdmin **bypasses** all tenant query filters
- Can access and query data from **all tenants**
- Row-Level Security (RLS) is **disabled** for SuperAdmin

### 3. Platform-Level Access
- Manages tenants (create, update, suspend, cancel)
- Manages subscription plans and features
- Manages users across all tenants
- System-wide monitoring and analytics

## Implementation Details

### 1. User Entity
```csharp
public class User : IdentityUser
{
    public string? TenantId { get; set; } // null for SuperAdmin
    // ... other properties
}
```

### 2. Query Filters (RLS)
All tenant-scoped entities have conditional query filters:
```csharp
entity.HasQueryFilter(e => IsSuperAdmin() || e.TenantId == _tenantProvider.TenantId);
```

### 3. Tenant Resolution Middleware
SuperAdmin bypasses tenant resolution:
```csharp
if (context.User?.IsSuperAdmin() == true)
{
    context.Items["TenantId"] = "platform";
    context.Items["IsSuperAdmin"] = true;
    await _next(context);
    return;
}
```

### 4. Feature Access Middleware
SuperAdmin bypasses feature checks:
```csharp
if (context.User?.IsSuperAdmin() == true)
{
    await _next(context);
    return;
}
```

### 5. JWT Token Claims
SuperAdmin gets special "platform" claim:
```csharp
if (isSuperAdmin)
{
    claims.Add(new Claim("TenantId", "platform"));
    claims.Add(new Claim("tenant", "platform"));
}
```

### 6. SaveChanges Behavior
SuperAdmin bypasses tenant ID enforcement:
```csharp
if (ShouldBypassTenantIsolation())
{
    // SuperAdmin can create/modify entities for any tenant
    return;
}
```

## SuperAdmin Extension Methods

```csharp
public static class SuperAdminExtensions
{
    public static bool IsSuperAdmin(this ClaimsPrincipal user)
    {
        return user?.IsInRole("SuperAdmin") == true;
    }

    public static bool ShouldBypassTenantIsolation(this ClaimsPrincipal user)
    {
        return user?.IsInRole("SuperAdmin") == true;
    }
}
```

## Platform-Level APIs

### 1. Tenant Management (`/api/tenants`)
- `GET /api/tenants` - List all tenants
- `GET /api/tenants/{id}` - Get tenant details
- `POST /api/tenants` - Create new tenant
- `PUT /api/tenants/{id}` - Update tenant
- `DELETE /api/tenants/{id}` - Delete tenant

**Authorization**: `[Authorize(Roles = "SuperAdmin")]`

### 2. User Management (`/api/usermanagement`)
- `GET /api/usermanagement/users` - List all users (all tenants)
- `GET /api/usermanagement/users/{id}` - Get user details
- `POST /api/usermanagement/users` - Create user
- `PUT /api/usermanagement/users/{id}` - Update user
- `DELETE /api/usermanagement/users/{id}` - Delete user

**Authorization**: `[Authorize(Roles = "SuperAdmin")]`

### 3. Subscription Plans (`/api/subscriptions/plans`)
- `GET /api/subscriptions/plans` - List all plans
- `POST /api/subscriptions/plans` - Create plan
- `PUT /api/subscriptions/plans/{id}` - Update plan

**Authorization**: `[Authorize(Roles = "SuperAdmin")]`

## Security Considerations

### 1. Authorization
- **All platform-level APIs require SuperAdmin role**
- Regular tenant users cannot access platform APIs
- Tenant isolation is enforced for non-SuperAdmin users

### 2. Query Filters
- Query filters are **bypassed** for SuperAdmin
- SuperAdmin sees **all tenants' data**
- Regular users only see their tenant's data

### 3. Tenant ID Enforcement
- SuperAdmin **does not have TenantId** set automatically
- SuperAdmin can **manually specify TenantId** when creating entities for tenants
- Regular users **must** have TenantId matching their tenant

### 4. Audit Logging
- All SuperAdmin actions should be logged
- Track which tenant's data was accessed
- Track what changes were made

## Usage Examples

### 1. SuperAdmin Accessing All Tenants' Data
```csharp
// SuperAdmin can query all employees
var allEmployees = await _context.Employees.ToListAsync(); // No tenant filter applied

// Regular user only sees their tenant's employees
var tenantEmployees = await _context.Employees.ToListAsync(); // Filtered by TenantId
```

### 2. SuperAdmin Creating Entity for Specific Tenant
```csharp
// SuperAdmin can create entities for any tenant
var employee = new Employee
{
    TenantId = "tenantname", // Specify tenant explicitly
    FirstName = "John",
    LastName = "Doe"
};
_context.Employees.Add(employee);
await _context.SaveChangesAsync(); // No tenant enforcement for SuperAdmin
```

### 3. SuperAdmin Managing Tenants
```csharp
[HttpPost]
[Authorize(Roles = "SuperAdmin")]
public async Task<IActionResult> CreateTenant(CreateTenantRequest request)
{
    var tenant = new Tenant
    {
        Name = request.Name,
        Domain = request.Domain,
        Status = TenantStatus.Provisioning
    };
    _context.Tenants.Add(tenant);
    await _context.SaveChangesAsync();
    return Ok(tenant);
}
```

## Best Practices

1. **Always check SuperAdmin role** before bypassing tenant isolation
2. **Log all SuperAdmin actions** for audit purposes
3. **Validate tenant access** even for SuperAdmin when targeting specific tenants
4. **Use platform-level APIs** instead of direct database access
5. **Monitor SuperAdmin usage** to detect potential security issues

## Testing

### SuperAdmin Test Scenarios
1. SuperAdmin can query all tenants' employees
2. SuperAdmin can create employees for any tenant
3. SuperAdmin bypasses feature checks
4. SuperAdmin can access all subscription plans
5. Regular users cannot access platform APIs

## Migration Notes

When migrating existing SuperAdmin users:
1. Set `TenantId = null` for all SuperAdmin users
2. Ensure JWT tokens don't include tenant claims for SuperAdmin
3. Verify query filters are bypassed correctly
4. Test platform-level APIs access

