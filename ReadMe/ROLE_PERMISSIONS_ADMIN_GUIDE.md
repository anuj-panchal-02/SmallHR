# Role Permissions Guide for Admin Users

## How Menu Access Works

The sidebar shows menu items based on **Role Permissions**. For an Admin user to see menu items in the sidebar:

1. **Modules** must exist for the tenant (created during tenant provisioning)
2. **Role Permissions** must exist with `canAccess = true` for the Admin role and the module's path
3. The sidebar filters modules using `canAccessPage(pagePath)` which checks permissions

## Method 1: Automatic (During Tenant Provisioning)

When a tenant is created, role permissions are automatically seeded for the Admin role:

**Location:** `SmallHR.Infrastructure/Services/TenantProvisioningService.cs`
- Method: `SeedTenantRolePermissionsAsync`

**Default Admin Permissions (all set to `true`):**
- `/dashboard` - Dashboard access
- `/employees` - Employee management
- `/departments` - Department management
- `/positions` - Positions management
- `/organization` - Organization structure

## Method 2: Using Role Permissions UI Page

1. **Login as Admin or SuperAdmin**
2. **Navigate to:** `/role-permissions` (Role Permissions page)
3. **Find the Admin role section**
4. **Toggle switches** for modules you want Admin to access
5. **Click "Save Changes"** to persist

**Note:** The Role Permissions page shows all modules and roles. You can enable/disable access for each role.

## Method 3: Using Initialize Permissions API

If permissions don't exist, you can initialize them:

**Endpoint:** `POST /api/rolepermissions/initialize`

**What it does:**
- Creates permissions for all roles (SuperAdmin, Admin, HR, Employee)
- Sets default permissions based on role hierarchy
- Admin gets full access (`canAccess = true`) to most pages

**Default Admin Permissions:**
- CanAccess: `true` for all pages
- CanView: `true`
- CanCreate: `true`
- CanEdit: `true`
- CanDelete: `true`

## Method 4: Check/Update Permissions via API

### Get All Permissions
```
GET /api/rolepermissions
```

### Get Admin Permissions Only
```
GET /api/rolepermissions/role/Admin
```

### Update Permission
```
PUT /api/rolepermissions/{id}
Body: {
  "canAccess": true,
  "canView": true,
  "canCreate": true,
  "canEdit": true,
  "canDelete": true
}
```

## How It Works Technically

1. **Frontend:** Sidebar component calls `canAccessPage(path)` from `useRolePermissions` hook
2. **Hook:** Checks `permissions` array from `useAuthStore`
3. **Permissions:** Loaded from `/api/rolepermissions/my-permissions` endpoint after login
4. **Filtering:** Sidebar only shows modules where `canAccessPage(module.path)` returns `true`

## Troubleshooting Empty Sidebar

If Admin user sees empty sidebar:

1. **Check if modules exist:**
   ```
   SELECT * FROM Modules WHERE TenantId = '{tenantId}'
   ```

2. **Check if permissions exist:**
   ```
   SELECT * FROM RolePermissions 
   WHERE RoleName = 'Admin' AND TenantId = '{tenantId}' AND CanAccess = 1
   ```

3. **Verify Admin role is assigned:**
   - Check `AspNetUserRoles` table
   - User should have `RoleId` matching Admin role

4. **Initialize permissions if missing:**
   - Use Role Permissions UI page
   - Or call `/api/rolepermissions/initialize` endpoint

## Quick Fix: Enable All Modules for Admin

If you need Admin to see all modules quickly:

1. Go to `/role-permissions` page
2. Find "Admin" role section
3. Enable the "Can Access" toggle for all modules you want Admin to see
4. Save changes

The sidebar will update immediately after permissions are saved.

