# Tenant Provisioning Guide

## Overview

SmallHR implements **automated tenant provisioning** that creates a complete tenant environment including:

- ✅ **Tenant Entry** - Tenant record in database
- ✅ **Subscription** - Automatic subscription creation (defaults to Free plan)
- ✅ **Schema/Database** - Tenant-specific data setup (for shared database)
- ✅ **Default Data** - Roles, permissions, modules, departments, positions
- ✅ **Admin User** - Tenant admin user with Admin role
- ✅ **Welcome Email** - Email with login details and password setup link

The provisioning system supports both **asynchronous background processing** and **synchronous API calls** for different use cases.

---

## Architecture

### Provisioning Flow

```
1. Create Tenant (POST /api/tenants)
   ↓
2. Tenant Status = Provisioning
   ↓
3. Background Worker or API Call
   ↓
4. Provisioning Steps:
   ├─ Create Subscription (Free plan by default)
   ├─ Seed Roles (SuperAdmin, Admin, HR, Employee)
   ├─ Seed Modules (Dashboard, Employees, Organization, etc.)
   ├─ Seed Departments & Positions
   ├─ Seed Role Permissions
   ├─ Create Admin User
   ├─ Assign Admin Role
   └─ Send Welcome Email
   ↓
5. Tenant Status = Active
```

### Background Worker

The `TenantProvisioningHostedService` automatically processes tenants with `Status = Provisioning`:

- Checks every 10 seconds for pending tenants
- Processes up to 5 tenants at a time
- Updates tenant status to `Active` on success or `Failed` on error
- Logs all provisioning steps for monitoring

---

## API Endpoints

### 1. Create Tenant (Async Provisioning)

Creates tenant and triggers async provisioning:

```http
POST /api/tenants
Content-Type: application/json
Authorization: Bearer {superadmin_token}

{
  "name": "Acme Corporation",
  "domain": "acme.local",
  "adminEmail": "admin@acme.com",
  "adminFirstName": "John",
  "adminLastName": "Admin",
  "subscriptionPlan": "Free",
  "maxEmployees": 10,
  "isActive": true
}
```

**Response (202 Accepted)**:
```json
{
  "id": 1,
  "status": "Provisioning",
  "statusUrl": "/api/tenants/1/status"
}
```

**Next Steps**:
1. Tenant is created with `Status = Provisioning`
2. Background worker picks up provisioning automatically
3. Check status via `GET /api/tenants/{id}/status`

### 2. Provision Tenant Synchronously

Manually trigger provisioning for a tenant:

```http
POST /api/provisioning/{tenantId}
Content-Type: application/json
Authorization: Bearer {superadmin_token}

{
  "subscriptionPlanId": 2,  // Optional: defaults to Free plan
  "startTrial": false
}
```

**Response (200 OK)**:
```json
{
  "message": "Tenant provisioned successfully",
  "tenantId": 1,
  "tenantName": "Acme Corporation",
  "subscriptionId": 1,
  "adminEmail": "admin@acme.com",
  "adminUserId": "...",
  "emailSent": true,
  "stepsCompleted": [
    "Roles created/verified",
    "Subscription created (Plan ID: 1)",
    "Modules seeded",
    "Departments and positions seeded",
    "Role permissions seeded",
    "Admin user created",
    "Admin role assigned",
    "Welcome email sent"
  ]
}
```

### 3. Check Provisioning Status

Monitor provisioning progress:

```http
GET /api/tenants/{id}/status
```

**Response (200 OK)**:
```json
{
  "id": 1,
  "name": "Acme Corporation",
  "status": "Active",
  "provisionedAt": "2025-11-02T10:30:00Z",
  "failureReason": null
}
```

**Status Values**:
- `Provisioning` - Provisioning in progress
- `Active` - Provisioning completed successfully
- `Failed` - Provisioning failed (check `failureReason`)

---

## Provisioning Steps

### Step 1: Create Subscription

- Creates subscription for tenant (defaults to Free plan)
- Links subscription to tenant
- Sets billing period (defaults to Monthly)
- Optionally starts trial if plan supports it

### Step 2: Seed Roles

Creates global roles (if not exists):
- `SuperAdmin` - Full system access
- `Admin` - Tenant admin access
- `HR` - HR manager access
- `Employee` - Basic employee access

### Step 3: Seed Modules

Creates tenant-specific modules:
- `/dashboard` - Main dashboard
- `/employees` - Employee management
- `/organization` - Organization structure
- `/departments` - Department management
- `/positions` - Position management

### Step 4: Seed Departments & Positions

Creates default departments:
- People/HR
- Engineering
- Sales
- Finance
- Customer Support
- Operations

Creates default positions for each department.

### Step 5: Seed Role Permissions

Sets up role-based access control:
- **SuperAdmin**: Full access to all pages
- **Admin**: Full access (except SuperAdmin features)
- **HR**: View, Create, Edit (no Delete)
- **Employee**: View only

### Step 6: Create Admin User

- Creates user account with provided admin email
- Generates secure password reset token
- User must set password via welcome email link

### Step 7: Assign Admin Role

- Assigns `Admin` role to tenant admin user
- Enables full tenant management access

### Step 8: Send Welcome Email

Sends email with:
- Welcome message
- Tenant name
- Password setup link (using reset token)
- Login instructions

---

## Usage Examples

### Example 1: Create Tenant with Async Provisioning

```bash
# 1. Create tenant
curl -X POST http://localhost:5000/api/tenants \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "name": "Acme Corp",
    "domain": "acme.local",
    "adminEmail": "admin@acme.com",
    "adminFirstName": "John",
    "adminLastName": "Admin"
  }'

# Response: { "id": 1, "status": "Provisioning", "statusUrl": "/api/tenants/1/status" }

# 2. Check status (repeat until status = "Active")
curl http://localhost:5000/api/tenants/1/status

# Response: { "id": 1, "name": "Acme Corp", "status": "Active", ... }
```

### Example 2: Provision Tenant Synchronously

```bash
# 1. Create tenant first
curl -X POST http://localhost:5000/api/tenants \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "name": "Acme Corp",
    "adminEmail": "admin@acme.com"
  }'

# 2. Provision immediately (synchronous)
curl -X POST http://localhost:5000/api/provisioning/1 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "subscriptionPlanId": 2,
    "startTrial": true
  }'

# Response: { "message": "Tenant provisioned successfully", ... }
```

### Example 3: Provision with Custom Plan

```bash
# Create tenant with Basic plan subscription
curl -X POST http://localhost:5000/api/provisioning/1 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "subscriptionPlanId": 2,
    "startTrial": false
  }'
```

---

## Background Worker Configuration

The `TenantProvisioningHostedService` is registered in `Program.cs`:

```csharp
builder.Services.AddHostedService<TenantProvisioningHostedService>();
```

### Configuration Options

- **Check Interval**: Defaults to 10 seconds
- **Batch Size**: Processes up to 5 tenants at a time
- **Retry Logic**: Failed tenants are marked with `Failed` status

### Monitoring

Monitor provisioning via logs:

```
[INFO] Tenant Provisioning Hosted Service started
[INFO] Processing provisioning for tenant 1: Acme Corp
[INFO] Roles created/verified
[INFO] Subscription created (Plan ID: 1)
[INFO] Modules seeded
[INFO] Tenant 1 provisioned successfully
```

---

## Error Handling

### Failed Provisioning

If provisioning fails:

1. Tenant status is set to `Failed`
2. `FailureReason` field contains error message
3. Background worker continues processing other tenants
4. Admin can retry provisioning via API

### Common Errors

**Admin Email Missing**:
```
{
  "status": "Failed",
  "failureReason": "Admin email is required for provisioning"
}
```

**Subscription Plan Not Found**:
```
{
  "status": "Failed",
  "failureReason": "Subscription plan not found"
}
```

**User Creation Failed**:
```
{
  "status": "Failed",
  "failureReason": "Failed to create admin user"
}
```

### Retry Provisioning

To retry failed provisioning:

```http
POST /api/provisioning/{tenantId}
Authorization: Bearer {token}
```

---

## Email Configuration

### Welcome Email

The system sends a welcome email to the admin user with:

- **Subject**: "Welcome to [Tenant Name] - Set Up Your Account"
- **Body**: Includes password setup link using reset token
- **Expiration**: Password reset token expires after 24 hours

### Email Service

Configure email service in `appsettings.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@gmail.com",
    "SmtpPassword": "your-password",
    "FromEmail": "noreply@smallhr.com",
    "FromName": "SmallHR"
  }
}
```

**Development**: Uses `ConsoleEmailService` (logs to console)

**Production**: Replace with SMTP/SendGrid email service

---

## Database Schema

### Tenant Table Fields

- `Id` - Tenant ID
- `Name` - Tenant name
- `Domain` - Optional subdomain
- `Status` - Provisioning status (Provisioning, Active, Failed)
- `ProvisionedAt` - When provisioning completed
- `FailureReason` - Error message if failed
- `AdminEmail` - Admin user email
- `AdminFirstName` - Admin first name
- `AdminLastName` - Admin last name

### Provisioning Status Enum

```csharp
public enum TenantProvisioningStatus
{
    Provisioning = 0,
    Active = 1,
    Failed = 2
}
```

---

## Security Considerations

### Admin User Creation

- Admin user is created with temporary secure password
- User must set password via welcome email link
- Password reset token is time-limited (24 hours)

### Role Assignment

- Admin role is assigned automatically
- SuperAdmin role is never assigned automatically
- Roles are global (not tenant-specific)

### Email Security

- Password reset tokens are single-use
- Tokens expire after 24 hours
- Email is sent to verified admin email address

---

## Best Practices

### 1. Idempotency

- Provisioning is **idempotent** - safe to call multiple times
- Existing resources are not recreated
- Duplicate calls return success

### 2. Error Recovery

- Failed provisioning can be retried
- Check `FailureReason` for error details
- Fix issue and retry via API

### 3. Monitoring

- Monitor background worker logs
- Check tenant status regularly
- Set up alerts for failed provisioning

### 4. Testing

- Test provisioning in development first
- Verify all steps complete successfully
- Test email delivery (use console email in dev)

---

## Troubleshooting

### Provisioning Stuck in "Provisioning" Status

**Check**:
1. Background worker is running
2. Logs show any errors
3. Tenant has valid admin email

**Solution**: Restart background worker or call provisioning API directly

### Email Not Sent

**Check**:
1. Email service is configured
2. Email service is registered in DI
3. Check logs for email errors

**Solution**: Email failure doesn't block provisioning. Tenant is still usable.

### Subscription Not Created

**Check**:
1. Subscription plans exist in database
2. Free plan is seeded
3. Check logs for subscription errors

**Solution**: Provisioning continues without subscription (not critical)

---

## Next Steps

1. ✅ **Provisioning Service** - Automated tenant provisioning
2. ✅ **Background Worker** - Async provisioning processing
3. ✅ **API Endpoints** - Provisioning endpoints
4. ✅ **Error Handling** - Comprehensive error handling
5. ⏳ **Monitoring Dashboard** - UI for monitoring provisioning
6. ⏳ **Retry Logic** - Automatic retry for failed provisioning
7. ⏳ **Notification Webhooks** - Webhooks for provisioning events

---

## Summary

The tenant provisioning system provides:

✅ **Automated Setup** - Complete tenant environment created automatically  
✅ **Async Processing** - Background worker processes provisioning  
✅ **Synchronous API** - Manual provisioning for immediate setup  
✅ **Comprehensive Seeding** - All default data created automatically  
✅ **Email Integration** - Welcome email with login details  
✅ **Error Handling** - Failed provisioning marked with error details  
✅ **Monitoring** - Status endpoint for provisioning progress  

**Ready to use**: Create a tenant and provisioning happens automatically!

