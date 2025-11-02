# SuperAdmin Testing Guide

## SuperAdmin Credentials

- **Email**: `superadmin@smallhr.com`
- **Password**: `SuperAdmin@123`

---

## Quick Start

### 1. Start Backend API

```powershell
cd SmallHR.API
dotnet run
```

**Expected Output**: 
- API should start on `http://localhost:5000` or `https://localhost:5001`
- Swagger UI available at `http://localhost:5000` (root)

### 2. Start Frontend

```powershell
cd SmallHR.Web
npm run dev
```

**Expected Output**:
- Frontend should start on `http://localhost:5173`

---

## Testing Checklist

### ✅ 1. Login as SuperAdmin

1. Navigate to `http://localhost:5173`
2. Login with:
   - Email: `superadmin@smallhr.com`
   - Password: `SuperAdmin@123`
3. **Expected**: 
   - Login successful
   - Redirected to SuperAdmin Dashboard
   - Role badge shows "SuperAdmin"

### ✅ 2. SuperAdmin Dashboard

**Location**: `/admin/dashboard` or main page after login

**Test Points**:
- ✅ Dashboard loads with overview statistics
- ✅ Quick access cards visible:
  - Tenants Management
  - Billing Center
  - Alerts Hub
- ✅ Clicking each card navigates to respective pages

### ✅ 3. Tenants List Page

**Location**: `/admin/tenants`

**Test Points**:
- ✅ Tenant list loads (may be empty initially)
- ✅ Search functionality works
- ✅ Filter by status/plan works
- ✅ Sort functionality works
- ✅ Click on tenant row navigates to Tenant Detail
- ✅ Actions available:
  - View Details
  - Impersonate
  - Suspend/Resume (if applicable)

### ✅ 4. Tenant Detail Page

**Location**: `/admin/tenants/:id`

**Test Points**:
- ✅ Tenant information displays correctly:
  - Basic info (name, domain, status)
  - Subscription details
  - Usage metrics (users, employees, API requests)
- ✅ Tabs available:
  - Overview
  - Subscription History
  - Usage Metrics
  - Lifecycle Events
  - Audit Logs
- ✅ Impersonate button works
- ✅ Back button returns to tenants list

### ✅ 5. Impersonation

**Test Points**:
- ✅ Click "Impersonate" on a tenant
- ✅ **Expected**: 
  - Warning banner appears at top
  - Shows impersonated tenant name
  - Shows token expiration time
  - "Stop Impersonation" button available
- ✅ Navigation works while impersonating
- ✅ Click "Stop Impersonation" returns to SuperAdmin view
- ✅ Banner disappears after stopping

### ✅ 6. Billing Center

**Location**: `/admin/billing`

**Test Points**:
- ✅ Webhook events table loads
- ✅ Filters work:
  - Date range
  - Status (Processed/Pending/Failed)
  - Provider (Stripe/Paddle)
- ✅ Pagination works
- ✅ Reconcile button functions (may show empty if no data)
- ✅ Webhook event details display:
  - Event type
  - Provider
  - Status
  - Tenant/Subscription links
  - Timestamp

### ✅ 7. Alerts Hub

**Location**: `/admin/alerts`

**Test Points**:
- ✅ Alerts dashboard loads
- ✅ Statistics display:
  - Total alerts
  - Active alerts
  - High severity alerts
  - Payment failures
  - Resolved alerts
- ✅ Alerts table displays:
  - Alert type (PaymentFailure, Overage, Error, Suspension)
  - Severity (Low, Medium, High, Critical)
  - Status (Active, Resolved, Acknowledged)
  - Tenant information
  - Timestamp
- ✅ Filters work:
  - Status
  - Type
  - Severity
  - Tenant
- ✅ Actions available:
  - Acknowledge
  - Resolve (with notes)
- ✅ Alert statistics chart/breakdown displays

---

## Test Webhook Events (Optional)

### Using Postman or cURL

1. **Stripe Webhook Test**:
   ```powershell
   curl -X POST http://localhost:5000/api/webhooks/BillingWebhooks/stripe `
     -H "Content-Type: application/json" `
     -H "Stripe-Signature: test-signature" `
     -d '{
       "type": "invoice.payment_failed",
       "data": {
         "object": {
           "customer": "cus_test123",
           "subscription": "sub_test123",
           "attempt_count": 1
         }
       }
     }'
   ```

2. **Check Database**:
   ```sql
   SELECT * FROM WebhookEvents ORDER BY CreatedAt DESC;
   ```

3. **Check Billing Center**:
   - Refresh Billing Center page
   - New webhook event should appear

---

## Test Alert Creation

### Create Test Alerts via API

1. **Payment Failure Alert** (requires tenant with subscription):
   - Triggered automatically when payment fails
   - Can be tested via webhook (see above)

2. **Check Alerts Hub**:
   - New alerts should appear
   - Alert statistics should update

---

## Common Issues & Solutions

### Issue: Login fails with "Invalid credentials"
**Solution**: 
- Verify SuperAdmin exists: `sqlcmd -S "(localdb)\mssqllocaldb" -d SmallHRDb -Q "SELECT Email FROM AspNetUsers WHERE Email = 'superadmin@smallhr.com'"`
- Reset password if needed via database

### Issue: Pages show 403 Forbidden
**Solution**:
- Verify user has SuperAdmin role
- Check JWT token includes SuperAdmin claim
- Clear browser cookies and login again

### Issue: No tenants appear in list
**Solution**:
- This is expected if no tenants exist
- Create a test tenant via API or UI

### Issue: Webhook events not appearing
**Solution**:
- Check webhook endpoint is accessible
- Verify database connection
- Check StripeWebhookHandler logs

### Issue: Alerts not showing
**Solution**:
- Alerts are created automatically on events (payment failures, overages, suspensions)
- May need to trigger test events to see alerts

---

## API Endpoints to Test

### SuperAdmin Endpoints

1. **Get All Tenants**: `GET /api/admin/tenants`
2. **Get Tenant Details**: `GET /api/admin/tenants/:id`
3. **Impersonate Tenant**: `POST /api/admin/tenants/:id/impersonate`
4. **Get Webhook Events**: `GET /api/admin/billing/webhooks`
5. **Get Alerts**: `GET /api/admin/alerts`
6. **Get Metrics**: `GET /api/admin/metrics/overview`

### Test with Swagger

1. Navigate to `http://localhost:5000` (Swagger UI)
2. Click "Authorize" button
3. Enter JWT token from login response
4. Test each endpoint

---

## Verification Commands

### Check SuperAdmin User
```sql
SELECT u.Id, u.Email, u.FirstName, u.LastName, u.TenantId, r.Name as Role
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'superadmin@smallhr.com';
```

### Check Webhook Events
```sql
SELECT TOP 10 
    Id, EventType, Provider, Processed, CreatedAt, TenantId, SubscriptionId
FROM WebhookEvents
ORDER BY CreatedAt DESC;
```

### Check Alerts
```sql
SELECT TOP 10 
    Id, TenantId, AlertType, Severity, Status, Message, CreatedAt
FROM Alerts
ORDER BY CreatedAt DESC;
```

### Check Tenants
```sql
SELECT TOP 10 
    Id, Name, Domain, Status, SubscriptionPlan, IsActive
FROM Tenants
ORDER BY CreatedAt DESC;
```

---

## Next Steps After Testing

1. ✅ Verify all pages load correctly
2. ✅ Test all CRUD operations (if applicable)
3. ✅ Test impersonation flow
4. ✅ Verify alerts are created on events
5. ✅ Test webhook event storage
6. ✅ Check error handling
7. ✅ Verify SuperAdmin actions are logged in AdminAudit

---

## Notes

- All SuperAdmin actions are automatically logged in `AdminAudit` table
- Webhook events are saved BEFORE processing (even if processing fails)
- Alerts are created automatically on certain events (payment failures, overages, suspensions)
- Impersonation tokens are short-lived (expire after configured time)

---

**Ready to test?** Start with step 1 above!

