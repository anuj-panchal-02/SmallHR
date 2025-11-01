# 🔐 Role-Based Access Control (RBAC) Setup Guide

## 🚨 Current Status

If you're seeing that RBAC only works for SuperAdmin, it means **permissions haven't been initialized** in the database yet.

## ✅ Quick Fix (Step-by-Step)

### **Step 1: Login as SuperAdmin**
```
Email: superadmin@smallhr.com
Password: Super@123
```

### **Step 2: Navigate to Role Permissions**
- Look in the sidebar under **"System"** section
- Click **"Role Permissions"**

### **Step 3: Initialize Permissions**
- Click the **"Initialize Permissions"** button
- This creates default permission entries for all roles and pages

### **Step 4: Review Default Permissions**
The table will show all pages with toggles for each role:
- ✅ Green = Access Granted
- ❌ Red = Access Denied

### **Step 5: Customize Permissions (Optional)**
Click the toggles to set which roles can access which pages.

**Recommended Setup:**
```
Page              | SuperAdmin | Admin | HR  | Employee
------------------+------------+-------+-----+---------
Dashboard         | ✅          | ✅    | ✅  | ✅
Departments       | ✅          | ✅    | ✅  | ❌
Employees         | ✅          | ✅    | ✅  | ❌
Calendar          | ✅          | ✅    | ✅  | ✅
Notice Board      | ✅          | ✅    | ✅  | ✅
Expenses          | ✅          | ✅    | ❌  | ❌
Payroll           | ✅          | ✅    | ❌  | ❌
Settings          | ✅          | ❌    | ❌  | ❌
Role Permissions  | ✅          | ❌    | ❌  | ❌
```

### **Step 6: Save Changes**
- Click **"Save Changes"** button (bottom right)
- Wait for success message

### **Step 7: Test**
- Logout
- Login as different roles
- Check that sidebar only shows authorized pages

---

## 🔍 Debugging

### **Check Browser Console**
Press `F12` and look for these logs:
```
[RBAC] Loaded X permissions for role: Admin
[RBAC] Access denied to /expenses for role Employee
```

### **Fallback Behavior**
If no permissions are found:
- Users will only see **Dashboard** in sidebar
- This is intentional to prevent complete lockout

### **Common Issues**

#### **Issue 1: Sidebar is empty (except dashboard)**
**Cause:** Permissions not initialized  
**Fix:** Follow steps 1-6 above

#### **Issue 2: Changes not saving**
**Cause:** Backend API not running  
**Fix:** Check that `http://localhost:5192` is accessible

#### **Issue 3: All roles see everything**
**Cause:** All toggles are green  
**Fix:** Review permissions and turn off access for specific role-page combinations

---

## 📊 API Endpoints Used

```
GET  /api/RolePermissions/role/{role}     - Get permissions for a role
POST /api/RolePermissions/initialize      - Initialize default permissions
PUT  /api/RolePermissions/bulk            - Save multiple permission changes
```

---

## 🎯 How It Works

1. **User logs in** → Role is stored in auth token
2. **App loads** → `useRolePermissions` hook fetches permissions from API
3. **Sidebar renders** → Filters menu items based on `canAccessPage()`
4. **Route accessed** → `ProtectedRoute` checks permission, redirects if denied

---

## 💡 Tips

- **SuperAdmin** always has access to everything (hardcoded)
- **Permissions are role-based**, not user-based
- **Changes are immediate** after saving (may need to refresh)
- **Use "Reset All"** to revoke all permissions and start fresh
- **Use "Refresh"** to reload permissions from database

---

## 🔄 Current Fallback Logic

```typescript
// If no permissions in database:
if (permissions.length === 0) {
  return pagePath === '/dashboard'; // Only allow dashboard
}
```

This ensures users can still access their dashboard even if permissions aren't set up yet.

---

## ✅ Success Indicators

You'll know RBAC is working when:
1. Different roles see different sidebar menus
2. Console shows "[RBAC] Loaded X permissions for role: Y"
3. Unauthorized page access redirects to dashboard
4. No errors in console

---

**Need Help?** Check the browser console for detailed [RBAC] logs!

