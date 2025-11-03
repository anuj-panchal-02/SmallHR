# Tenant Impersonation Guide

## What is Tenant Impersonation?

Tenant impersonation allows **SuperAdmin** users to temporarily view and interact with the system as if they were logged in as a specific tenant. This is useful for:
- **Customer Support**: Help troubleshoot tenant-specific issues
- **Testing**: Verify tenant experiences and features
- **Training**: Demonstrate tenant-specific functionality
- **Troubleshooting**: Debug issues that only appear for specific tenants

## How Impersonation Works

### 1. **Starting Impersonation**
- SuperAdmin navigates to **Tenant Management** (`/admin/tenants`)
- Selects a tenant and clicks **"Impersonate Tenant"**
- System generates a short-lived JWT token (default: 30 minutes)
- Token includes:
  - Impersonated tenant's `TenantId` claim
  - `IsImpersonating: "true"` flag
  - Original SuperAdmin's ID and email (for audit trail)
  - SuperAdmin role (retained for security)

### 2. **During Impersonation**

#### **What SuperAdmin Can Do:**
✅ **View Tenant Data**
- See all employees, departments, positions for that tenant
- View leave requests, attendance records
- Access tenant-specific dashboards and reports
- See tenant's subscription and billing information

✅ **Access Tenant Features**
- Navigate tenant's module structure
- Use tenant-specific permissions and role-based access
- View tenant's usage metrics
- Access all pages available to that tenant

✅ **Query Filtering**
- Database queries automatically filter to show only impersonated tenant's data
- `TenantResolutionMiddleware` sets tenant context from JWT claims
- Global query filters apply tenant isolation

✅ **Still Has SuperAdmin Powers**
- Can access SuperAdmin-only features if needed
- Role is retained in JWT token for security
- Original identity tracked in audit logs

#### **What Happens on Frontend:**
- **Impersonation Banner** appears at top of every page showing:
  - Tenant name being impersonated
  - Countdown timer (minutes remaining)
  - "Stop Impersonation" button
- User is **redirected to tenant's dashboard**
- All API requests use impersonation token
- Tenant context is set automatically

### 3. **Security & Audit Trail**

#### **All Actions Are Logged:**
- **Admin Audit Trail**: Every action is logged with:
  - Original SuperAdmin's ID and email
  - Impersonated tenant ID
  - Action type (e.g., "Employee.Create", "Department.Update")
  - Timestamp and endpoint
  - IP address and user agent

#### **Token Expiration:**
- Default duration: **30 minutes** (configurable)
- Token automatically expires after duration
- User must re-authenticate as SuperAdmin to continue
- Banner shows countdown timer

#### **Stop Impersonation:**
- Click **"Stop Impersonation"** button in banner
- Or wait for token to expire
- Returns to SuperAdmin view
- Original SuperAdmin token restored

## What You CAN Do While Impersonating

### ✅ **Tenant-Specific Operations**
1. **View & Manage Employees**
   - View employee list (filtered to impersonated tenant)
   - Create, edit, delete employees
   - View employee details
   - Search and filter employees

2. **Manage Departments & Positions**
   - View/create/edit departments
   - View/create/edit positions
   - See department/position assignments

3. **Leave Requests Management**
   - View all leave requests for tenant
   - Approve/reject leave requests
   - Create leave requests
   - View leave history

4. **Attendance Tracking**
   - View attendance records
   - Clock in/out for employees
   - View attendance reports
   - Track hours and overtime

5. **Access Tenant Dashboard**
   - View tenant-specific statistics
   - See tenant's usage metrics
   - View tenant's subscription plan
   - Access tenant's billing information

6. **Role-Based Access Control**
   - Respect tenant's role permissions
   - Access features based on tenant's subscription plan
   - View tenant's module structure

### ✅ **SuperAdmin Features (Still Available)**
- Access SuperAdmin dashboard
- View tenant management features
- Access billing and subscription management
- View system alerts and metrics

## What You CANNOT Do While Impersonating

### ❌ **Cross-Tenant Access**
- Cannot view other tenants' data (unless explicitly using SuperAdmin features)
- Query filters enforce tenant isolation
- Cannot access data from other tenants without stopping impersonation

### ❌ **Modify Tenant Settings**
- Some tenant-level settings may be restricted
- Cannot change tenant's subscription directly (use SuperAdmin features)
- Cannot modify tenant's domain or core settings

## Technical Details

### **Token Structure**
```json
{
  "TenantId": "123",
  "IsImpersonating": "true",
  "OriginalUserId": "superadmin-id",
  "OriginalEmail": "superadmin@example.com",
  "roles": ["SuperAdmin"]
}
```

### **Middleware Behavior**
- `TenantResolutionMiddleware` detects impersonation flag
- Sets `HttpContext.Items["TenantId"]` to impersonated tenant
- Sets `HttpContext.Items["IsImpersonating"]` to `true`
- Stores original user info for audit trail

### **Database Query Filters**
- Global query filters apply tenant isolation
- SuperAdmin can bypass filters if needed (explicit `IgnoreQueryFilters()`)
- Queries automatically filter by impersonated tenant's ID

## Best Practices

### ✅ **Do:**
- Use impersonation for **support and troubleshooting**
- **Stop impersonation** when done
- **Review audit logs** regularly
- **Document** why impersonation was needed
- **Respect tenant privacy** and data boundaries

### ❌ **Don't:**
- Leave impersonation active unnecessarily
- Modify tenant data without justification
- Share impersonation tokens
- Use impersonation to bypass security checks
- Impersonate without tenant consent (for support scenarios)

## Example Use Cases

### 1. **Customer Support**
> "A tenant reports they can't see their employees. SuperAdmin impersonates the tenant to verify their experience and troubleshoot the issue."

### 2. **Testing New Features**
> "SuperAdmin impersonates a tenant to test how a new feature appears in their dashboard."

### 3. **Data Migration Verification**
> "After migrating tenant data, SuperAdmin impersonates to verify all data was migrated correctly."

### 4. **Training & Demos**
> "SuperAdmin impersonates a tenant to demonstrate features during onboarding or training sessions."

## Security Considerations

### **Audit Trail**
- All actions while impersonating are logged with:
  - Who (original SuperAdmin)
  - What (action performed)
  - When (timestamp)
  - Which tenant (impersonated tenant)
  - IP address and user agent

### **Token Security**
- Impersonation tokens are JWT tokens signed with secret key
- Tokens expire automatically (default: 30 minutes)
- Tokens are stored in `localStorage` (frontend) and cookies (backend)

### **Access Control**
- Only **SuperAdmin** role can initiate impersonation
- Cannot impersonate inactive or suspended tenants
- All API requests validated with token signature

## API Endpoints

### **Start Impersonation**
```
POST /api/admin/tenants/{tenantId}/impersonate?durationMinutes=30
```

### **Stop Impersonation**
```
POST /api/admin/tenants/stop-impersonation
```

## Summary

**Impersonation allows SuperAdmin to:**
- ✅ View tenant's data and experience their interface
- ✅ Access tenant-specific features and pages
- ✅ Troubleshoot tenant-specific issues
- ✅ Test features from tenant's perspective
- ✅ Maintain audit trail of all actions
- ✅ Stop impersonation at any time

**Impersonation does NOT allow:**
- ❌ Bypass security or audit logging
- ❌ Access other tenants' data simultaneously
- ❌ Modify tenant settings without proper authorization
- ❌ Bypass role-based access control (still respects tenant's permissions)

