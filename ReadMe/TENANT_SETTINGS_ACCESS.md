# Tenant Settings Access Guide

## Overview
Tenant Settings is now available for SuperAdmins to manage tenant subscriptions and employee limits in your SaaS-based SmallHR system.

## What Was Added

### Backend Changes
1. **Module**: Added "Tenant Settings" module to `/tenant-settings` path
2. **Permissions**: SuperAdmin has full access to Tenant Settings; Admin, HR, and Employee are blocked
3. **Endpoints**: All existing tenant management endpoints remain available

### Frontend Changes
1. **Page**: Created `TenantSettings.tsx` with full tenant management UI
2. **Route**: Mapped `/tenant-settings` to TenantSettings component
3. **Icon**: Added ApartmentOutlined icon for tenant pages in sidebar

## Setup Instructions

### For New Installations
The Tenant Settings module will be automatically seeded when the API starts for the first time.

### For Existing Installations
You need to add the Tenant Settings module to your existing database:

**Option 1: Use Add Missing Modules Endpoint**
```bash
POST http://localhost:5192/api/modules/add-missing
```
This will add the Tenant Settings module if it's missing.

**Option 2: Use Add Missing Permissions Endpoint**
```bash
POST http://localhost:5192/api/rolepermissions/add-missing
```
This will add the Tenant Settings permissions for all roles (only SuperAdmin will have access).

**Option 3: Add via Role Permissions Page**
Navigate to `/role-permissions` in the frontend and click "Add Missing Modules" button, then "Add Missing Permissions".

## Access Control

### SuperAdmin
- ✅ Full access to Tenant Settings
- ✅ Can create, view, edit, and delete tenants
- ✅ Can manage subscription plans and employee limits
- ✅ Can update domains and active status

### Admin, HR, Employee
- ❌ No access to Tenant Settings
- ❌ Cannot see Tenant Settings in sidebar
- ❌ Cannot navigate to `/tenant-settings` URL

## Features

### Tenant Management
- View all tenants in a table
- Create new tenants with subscription plans
- Edit tenant details (name, domain, subscription)
- Delete tenants
- Toggle active/inactive status
- Update subscription plans dynamically

### Subscription Plans
- **Free**: 10 employees maximum
- **Basic**: 50 employees maximum  
- **Pro**: 200 employees maximum
- **Enterprise**: 1000 employees maximum

### Employee Limit Enforcement
- Automatically enforced when creating employees
- Clear error messages when limit is reached
- Suggests upgrading subscription plan

## Testing Access

1. **Login as SuperAdmin**: `superadmin@smallhr.com` / `SuperAdmin@123`
2. **Check Sidebar**: You should see "Tenant Settings" menu item
3. **Navigate**: Click on "Tenant Settings" or go to `/tenant-settings`
4. **Verify**: You should see the tenant management interface

## Troubleshooting

### Tenant Settings Not Visible
1. Run `POST /api/modules/add-missing` to add the module
2. Refresh the frontend page
3. Check browser console for errors

### Can't Access Even as SuperAdmin
1. Run `POST /api/rolepermissions/add-missing` to add permissions
2. Logout and login again to refresh token
3. Verify you're logged in as SuperAdmin role

### Build Warnings (File Locked)
The API process is running and locking DLLs. This is normal in development. The build succeeded anyway.

## Files Modified

**Backend:**
- `SmallHR.API/Controllers/ModulesController.cs` - Added Tenant Settings module
- `SmallHR.API/Controllers/RolePermissionsController.cs` - Added Tenant Settings permissions
- `SmallHR.API/Controllers/TenantsController.cs` - Subscription management endpoints (already existed)

**Frontend:**
- `SmallHR.Web/src/pages/TenantSettings.tsx` - **NEW** Tenant management page
- `SmallHR.Web/src/App.tsx` - Added route mapping
- `SmallHR.Web/src/components/Layout/Sidebar.tsx` - Added apartment icon

---

**Status**: ✅ Complete
**Access**: SuperAdmin only
**Feature**: SaaS subscription management with employee limits


