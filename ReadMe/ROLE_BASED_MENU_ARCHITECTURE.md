# Role-Based Menu Architecture

## Overview

SmallHR implements a sophisticated role-based menu system where navigation menus are **loaded from the database** and **filtered by user permissions**. This ensures dynamic menu management and consistent access control across the application.

---

## Architecture Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        DATABASE LAYER                            │
├─────────────────────────────────────────────────────────────────┤
│  Modules Table              RolePermissions Table               │
│  ──────────────            ──────────────────────               │
│  • All menu items          • Access per role per page           │
│  • Hierarchical structure  • CanAccess, CanView, etc.           │
│  • Tenant-specific         • Tenant-specific                    │
└─────────────────────────────────────────────────────────────────┘
                              ↓                    ↓
┌─────────────────────────────────────────────────────────────────┐
│                        BACKEND API LAYER                         │
├─────────────────────────────────────────────────────────────────┤
│  GET /api/modules          GET /api/rolepermissions/my-perms   │
│  • Returns all modules     • Returns current user's permissions │
│  • Hierarchical structure  • Based on JWT role claim            │
│  • Tenant-filtered         • Filtered by role                   │
└─────────────────────────────────────────────────────────────────┘
                              ↓                    ↓
┌─────────────────────────────────────────────────────────────────┐
│                     FRONTEND LAYER                               │
├─────────────────────────────────────────────────────────────────┤
│  useModulesStore           useRolePermissions                   │
│  • Fetches all modules     • Fetches user permissions           │
│  • Caches navigation data  • Caches access control data         │
└─────────────────────────────────────────────────────────────────┘
                              ↓                    ↓
┌─────────────────────────────────────────────────────────────────┐
│                  SIDEBAR COMPONENT                               │
├─────────────────────────────────────────────────────────────────┤
│  • Receives all modules                                         │
│  • Filters using canAccessPage(module.path)                     │
│  • Only displays accessible items                               │
│  • Supports hierarchical/collapsible menus                      │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                  PROTECTED ROUTE                                 │
├─────────────────────────────────────────────────────────────────┤
│  • Receives route path                                          │
│  • Checks canAccessPage(route.path)                             │
│  • Shows AccessDenied if unauthorized                           │
│  • Allows access if authorized                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Database Schema

### Modules Table

Defines the **structure** of all menu items in the system:

| Column | Type | Description |
|--------|------|-------------|
| `TenantId` | string | Multi-tenant identifier |
| `Name` | string | Display name (e.g., "Dashboard", "Employees") |
| `Path` | string | Route path (e.g., "/dashboard", "/employees") |
| `ParentPath` | string? | Parent module path for hierarchy |
| `Icon` | string? | Icon name/class |
| `DisplayOrder` | int | Sort order within parent |
| `IsActive` | bool | Show/hide in menu |
| `Description` | string? | Tooltip/description |

**Example Records:**
```sql
INSERT INTO Modules VALUES 
('default', 'Dashboard', '/dashboard', NULL, 'dashboard', 1, 1, 'Overview'),
('default', 'Employees', '/employees', NULL, 'user', 2, 1, 'Manage employees'),
('default', 'Organization', '/organization', NULL, 'team', 3, 1, 'Org structure'),
('default', 'Departments', '/departments', '/organization', 'team', 1, 1, 'Manage departments'),
('default', 'Positions', '/positions', '/organization', 'user', 2, 1, 'Manage positions');
```

### RolePermissions Table

Defines **access control** per role per page:

| Column | Type | Description |
|--------|------|-------------|
| `TenantId` | string | Multi-tenant identifier |
| `RoleName` | string | Role (SuperAdmin, Admin, HR, Employee) |
| `PagePath` | string | Matches Module.Path |
| `PageName` | string | Display name |
| `CanAccess` | bool | **Menu visibility & page access** |
| `CanView` | bool | View-only actions |
| `CanCreate` | bool | Create operations |
| `CanEdit` | bool | Edit operations |
| `CanDelete` | bool | Delete operations |
| `Description` | string? | Additional context |

**Example Records:**
```sql
INSERT INTO RolePermissions VALUES 
('default', 'SuperAdmin', '/dashboard', 'Dashboard', 1, 1, 1, 1, 1, NULL),
('default', 'HR', '/dashboard', 'Dashboard', 1, 1, 0, 0, 0, NULL),
('default', 'Employee', '/dashboard', 'Dashboard', 1, 1, 0, 0, 0, NULL),
('default', 'HR', '/employees', 'Employees', 1, 1, 1, 1, 0, NULL),
('default', 'Employee', '/employees', 'Employees', 0, 0, 0, 0, 0, NULL);
```

---

## Backend Implementation

### ModulesController.cs

```csharp
[HttpGet]
[Authorize]
public async Task<IActionResult> GetModules()
{
    var modules = await _db.Modules
        .AsNoTracking()
        .Where(m => m.IsActive && !m.IsDeleted)
        .OrderBy(m => m.ParentPath)
        .ThenBy(m => m.DisplayOrder)
        .ToListAsync();

    // Build hierarchical tree
    var byPath = modules.ToDictionary(m => m.Path, ...);
    var roots = new List<object>();
    // ... tree building logic ...
    
    return Ok(roots);
}
```

**Key Points:**
- Returns **all modules** (no role filtering here)
- Automatically filters by `TenantId` via EF Core query filters
- Builds hierarchical structure based on `ParentPath`
- Orders by `DisplayOrder` within each level

### RolePermissionsController.cs

```csharp
[HttpGet("my-permissions")]
public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetMyPermissions()
{
    // Extract role from JWT token
    var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
    
    var permissions = await _context.RolePermissions
        .Where(p => p.RoleName == userRole)
        .ToListAsync();
    
    return Ok(permissions);
}
```

**Key Points:**
- Extracts role from JWT claims
- Returns only permissions for the user's role
- Used by frontend for access control

---

## Frontend Implementation

### Module Fetching (useModulesStore)

```typescript
// SmallHR.Web/src/store/modulesStore.ts
export const useModulesStore = create<ModulesState>((set) => ({
  modules: [],
  loading: false,
  refresh: async () => {
    const mods = await fetchModulesForCurrentUser();
    set({ modules: mods, loading: false });
  },
}));
```

**Flow:**
1. Fetches all modules from `/api/modules`
2. Stores complete menu structure
3. Caches in Zustand store

### Permission Fetching (useAuthStore)

```typescript
// SmallHR.Web/src/store/authStore.ts
fetchPermissions: async () => {
  const response = await axios.get(
    'http://localhost:5192/api/RolePermissions/my-permissions',
    { withCredentials: true }
  );
  
  set({ permissions: response.data, permissionsLoaded: true });
}
```

**Flow:**
1. Called after successful login
2. Fetches user's role-based permissions
3. Stores for use by `useRolePermissions` hook

### Access Control Hook (useRolePermissions)

```typescript
// SmallHR.Web/src/hooks/useRolePermissions.ts
const canAccessPage = (pagePath: string): boolean => {
  // SuperAdmin shortcut
  if (user?.roles?.[0] === 'SuperAdmin') return true;
  
  // Find permission for this page
  const permission = permissions.find(p => p.pagePath === pagePath);
  return permission?.canAccess ?? false;
};
```

**Key Points:**
- SuperAdmin bypasses all checks
- Uses stored permissions to determine access
- Returns `false` for unknown pages (secure by default)

### Sidebar Filtering

```typescript
// SmallHR.Web/src/components/Layout/Sidebar.tsx
const menuSections: MenuSection[] = flattenToSections(modules);

{menuSections.map((section) => {
  const accessibleItems = section.items.filter(item => 
    canAccessPage(item.path)  // ← FILTER HERE
  );
  
  if (accessibleItems.length === 0) return null; // ← Don't render empty sections
  
  return (
    <div key={section.title}>
      {accessibleItems.map((item) => (
        <MenuItem key={item.key} item={item} />
      ))}
    </div>
  );
})}
```

**Key Points:**
- Receives all modules from store
- Filters each item using `canAccessPage()`
- Only renders accessible items
- Preserves hierarchical structure

### Route Protection

```typescript
// SmallHR.Web/src/components/ProtectedRoute.tsx
export const ProtectedRoute: React.FC<{ requiredPath: string }> = ({ requiredPath }) => {
  const { canAccessPage, loading } = useRolePermissions();
  
  if (!canAccessPage(requiredPath)) {
    return <AccessDenied requiredPath={requiredPath} />;
  }
  
  return <>{children}</>;
};
```

**Key Points:**
- Blocks unauthorized direct URL access
- Shows friendly Access Denied page
- Prevents user from seeing restricted pages

---

## Default Role Permissions

### SuperAdmin
| Page | CanAccess | CanView | CanCreate | CanEdit | CanDelete |
|------|-----------|---------|-----------|---------|-----------|
| All pages | ✅ | ✅ | ✅ | ✅ | ✅ |

### Admin
| Page | CanAccess | CanView | CanCreate | CanEdit | CanDelete |
|------|-----------|---------|-----------|---------|-----------|
| Dashboard | ✅ | ✅ | ✅ | ✅ | ✅ |
| Employees | ✅ | ✅ | ✅ | ✅ | ✅ |
| Department | ✅ | ✅ | ✅ | ✅ | ✅ |
| Calendar | ✅ | ✅ | ✅ | ✅ | ✅ |
| Notice Board | ✅ | ✅ | ✅ | ✅ | ✅ |
| Expenses | ✅ | ✅ | ✅ | ✅ | ✅ |
| Payroll | ✅ | ✅ | ✅ | ✅ | ✅ |
| Settings | ✅ | ✅ | ✅ | ✅ | ✅ |
| Role Permissions | ❌ | ❌ | ❌ | ❌ | ❌ |

### HR
| Page | CanAccess | CanView | CanCreate | CanEdit | CanDelete |
|------|-----------|---------|-----------|---------|-----------|
| Dashboard | ✅ | ✅ | ❌ | ❌ | ❌ |
| Employees | ✅ | ✅ | ✅ | ✅ | ❌ |
| Department | ❌ | ❌ | ❌ | ❌ | ❌ |
| Calendar | ✅ | ✅ | ✅ | ✅ | ❌ |
| Notice Board | ✅ | ✅ | ✅ | ✅ | ❌ |
| Expenses | ❌ | ❌ | ❌ | ❌ | ❌ |
| Payroll | ❌ | ❌ | ❌ | ❌ | ❌ |
| Settings | ✅ | ✅ | ❌ | ❌ | ❌ |
| Role Permissions | ❌ | ❌ | ❌ | ❌ | ❌ |

### Employee
| Page | CanAccess | CanView | CanCreate | CanEdit | CanDelete |
|------|-----------|---------|-----------|---------|-----------|
| Dashboard | ✅ | ✅ | ❌ | ❌ | ❌ |
| Employees | ❌ | ❌ | ❌ | ❌ | ❌ |
| Department | ❌ | ❌ | ❌ | ❌ | ❌ |
| Calendar | ✅ | ✅ | ❌ | ❌ | ❌ |
| Notice Board | ✅ | ✅ | ❌ | ❌ | ❌ |
| Expenses | ❌ | ❌ | ❌ | ❌ | ❌ |
| Payroll | ❌ | ❌ | ❌ | ❌ | ❌ |
| Settings | ✅ | ✅ | ❌ | ❌ | ❌ |
| Role Permissions | ❌ | ❌ | ❌ | ❌ | ❌ |

---

## Initialization

### First-Time Setup

1. **Seed Modules**: Automatically done by `Program.cs` on startup
   ```csharp
   // SeedDataAsync creates essential modules
   organizationModule = new Module { ... };
   departmentsModule = new Module { ... };
   positionsModule = new Module { ... };
   ```

2. **Initialize Permissions**: Manual step for RolePermissions
   ```bash
   # Login as SuperAdmin, navigate to Role Permissions page
   # Click "Initialize Permissions" button
   # OR use Swagger UI:
   POST /api/rolepermissions/initialize
   ```

3. **Verify Setup**:
   ```bash
   # Check modules exist
   GET /api/modules
   
   # Check permissions exist
   GET /api/rolepermissions/my-permissions
   ```

---

## Multi-Tenancy

Both `Modules` and `RolePermissions` tables are **tenant-isolated**:

- Automatically filtered by `TenantId` via EF Core query filters
- Each tenant has their own menu structure
- Each tenant has their own role permissions
- SuperAdmin can see/manage all tenants

---

## Benefits

✅ **Database-Driven**: Change menus without code deployment  
✅ **Role-Based**: Different users see different menus  
✅ **Multi-Tenant**: Each organization has custom menus  
✅ **Secure**: Frontend + Backend validation  
✅ **Performant**: Cached in frontend stores  
✅ **Maintainable**: Clear separation of concerns  

---

## Testing

### Test Role-Based Filtering

1. Login as SuperAdmin → Should see all menus
2. Login as HR → Should see limited menus
3. Login as Employee → Should see minimal menus
4. Try direct URL access to restricted page → Should show Access Denied

### Test Module Loading

```bash
# Get all modules
curl http://localhost:5192/api/modules -H "Authorization: Bearer $TOKEN"

# Get my permissions
curl http://localhost:5192/api/rolepermissions/my-permissions \
     -H "Authorization: Bearer $TOKEN"
```

---

## Troubleshooting

### Menus Not Showing

1. Check modules exist in database:
   ```sql
   SELECT * FROM Modules WHERE IsActive = 1;
   ```

2. Check permissions exist for role:
   ```sql
   SELECT * FROM RolePermissions WHERE RoleName = 'HR' AND CanAccess = 1;
   ```

3. Check tenant isolation:
   ```sql
   SELECT * FROM Modules WHERE TenantId = 'default';
   ```

### Access Denied Unexpectedly

1. Verify JWT token has correct role claim
2. Check RolePermissions record exists for that role+path
3. Ensure `CanAccess = 1` in database
4. Check tenant isolation filters

---

## Future Enhancements

- [ ] Dynamic module creation via UI
- [ ] Module-level permissions (not just page-level)
- [ ] Menu item visibility based on feature flags
- [ ] A/B testing different menu structures
- [ ] User-level menu customization

---

## References

- `SmallHR.API/Controllers/ModulesController.cs`
- `SmallHR.API/Controllers/RolePermissionsController.cs`
- `SmallHR.Web/src/components/Layout/Sidebar.tsx`
- `SmallHR.Web/src/hooks/useRolePermissions.ts`
- `SmallHR.Web/src/store/modulesStore.ts`
- `SmallHR.Web/src/store/authStore.ts`

