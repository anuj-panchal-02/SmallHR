# Company Onboarding & Login Creation Guide

This guide explains how a **company** (tenant) sets up their SmallHR account and creates logins for their team members.

---

## 🏢 Overview: Three Types of User Creation

SmallHR supports three distinct workflows for creating user accounts:

1. **Company Onboarding** - Creating a new tenant company account
2. **Employee Self-Registration** - Employees creating their own accounts
3. **Admin User Management** - Admins creating accounts for employees

---

## 1️⃣ Company Onboarding (New Tenant Setup)

### Scenario
A new company wants to use SmallHR. They need:
- A tenant account created
- A SuperAdmin user to manage the company
- Initial setup (modules, departments, etc.)

### Who Creates This?
**Option A: Manual Admin Setup (Current Implementation)**
- Your SmallHR platform administrator creates the tenant
- Provides company with access credentials

**Option B: Self-Service Signup (Recommended Enhancement)**
- Company registers themselves via `/signup` page
- Creates tenant + first admin user in one flow
- Receives email verification

### Current Implementation: Manual Setup

#### Step 1: Platform Admin Creates Tenant

**Endpoint**: `POST /api/tenants`

**Request** (requires platform admin credentials):
```json
{
  "name": "acme-corp",
  "domain": "acme.com",
  "isActive": true
}
```

**What happens**:
- Tenant record created in database
- Company gets unique tenant ID: `acme-corp`

#### Step 2: Seed Modules for Tenant

**Endpoint**: `POST /api/modules/seed`

**Headers**:
```
Authorization: Bearer <platform-admin-jwt>
X-Tenant-Id: acme-corp
```

**What happens**:
- Default navigation modules created
- Organization structure initialized
- Company-specific permissions set up

#### Step 3: Create Company's First Admin

**Endpoint**: `POST /api/usermanagement/create-user`

**Headers**:
```
Authorization: Bearer <platform-admin-jwt>
```

**Request**:
```json
{
  "email": "ceo@acme.com",
  "password": "SecurePass123!@#",
  "firstName": "John",
  "lastName": "Doe",
  "dateOfBirth": "1980-01-15",
  "role": "Admin"
}
```

**What happens**:
- User account created
- Assigned "Admin" role
- Account is active and ready to use

#### Step 4: Login Setup

**Credentials Provided to Company**:
```
Email: ceo@acme.com
Password: SecurePass123!@#
Initial Login URL: https://your-saas.com/login?tenant=acme-corp
```

**Company Admin Login Flow**:
1. Navigate to login page
2. Enter credentials
3. System automatically associates with `acme-corp` tenant
4. Access dashboard with company-specific data

---

## 2️⃣ Employee Self-Registration

### Scenario
An existing employee of the company wants to create their own account.

### Current Implementation

**Endpoint**: `POST /api/auth/register`

**Request**:
```json
{
  "email": "employee@acme.com",
  "password": "SecurePass456!@#",
  "firstName": "Jane",
  "lastName": "Smith",
  "dateOfBirth": "1990-05-20",
  "address": "123 Main St",
  "city": "San Francisco",
  "state": "CA",
  "zipCode": "94102",
  "country": "USA"
}
```

**Headers**:
```
X-Tenant-Id: acme-corp
```

**What happens**:
1. User account created in `acme-corp` tenant
2. Assigned default role: **"Employee"**
3. JWT token generated
4. Tokens stored in httpOnly cookies
5. User immediately logged in

**Result**: Employee can now log in with their credentials

---

## 3️⃣ Admin Creating User Accounts

### Scenario
Company admin (Admin role) needs to create accounts for:
- New employees
- HR managers
- Department heads
- Other team members

### Current Implementation

**Endpoint**: `POST /api/usermanagement/create-user`

**Authorization**: Requires **SuperAdmin** role (platform-level admin)

**Request**:
```json
{
  "email": "hr-manager@acme.com",
  "password": "TemporaryPass789!@#",
  "firstName": "Alice",
  "lastName": "Johnson",
  "dateOfBirth": "1985-08-10",
  "role": "HR"
}
```

**What happens**:
1. Account created in tenant database
2. User assigned specified role (HR, Admin, Employee, etc.)
3. User receives temporary password
4. **Security**: User must change password on first login (recommended)

**Note**: Currently only SuperAdmin can create users. Enhancement needed to allow tenant-level Admin to create users.

---

## 🔄 Login Workflow After Account Creation

### Standard Login Flow

**Endpoint**: `POST /api/auth/login`

**Request**:
```json
{
  "email": "employee@acme.com",
  "password": "SecurePass456!@#"
}
```

**Headers**:
```
X-Tenant-Id: acme-corp
```

**Response**:
```json
{
  "expiration": "2024-01-15T14:30:00Z",
  "user": {
    "id": "user-id-123",
    "email": "employee@acme.com",
    "firstName": "Jane",
    "lastName": "Smith",
    "roles": ["Employee"],
    "isActive": true
  }
}
```

**Security**:
- JWT token stored in httpOnly cookie
- Refresh token stored in httpOnly cookie
- Tokens automatically attached to subsequent requests
- Tokens valid for 60 minutes (access) / 7 days (refresh)

---

## 📋 Comparison: Three User Creation Methods

| Method | Who Uses It | Default Role | Requires Approval | Immediate Login |
|--------|-------------|--------------|-------------------|-----------------|
| **Public Registration** | Employee self-register | Employee | ❌ No | ✅ Yes |
| **Admin Creates User** | SuperAdmin creating accounts | Specified | ❌ No | ❌ Manual login |
| **Company Onboarding** | New tenant setup | Admin | ✅ Platform Admin | ❌ Manual login |

---

## 🎯 Recommended Company Onboarding Flow

### Current State: Manual Platform Admin Setup

```
Company Requests Access
        ↓
Platform Admin Creates Tenant
        ↓
Platform Admin Seeds Modules
        ↓
Platform Admin Creates First Admin User
        ↓
Credentials Provided to Company
        ↓
Company Admin Logs In
        ↓
Company Admin Manages Their Organization
```

**Pros**:
- Controlled growth
- Quality control
- Approval workflow

**Cons**:
- Requires manual intervention
- Slower onboarding
- Scales poorly

### Recommended Enhancement: Self-Service Signup

```
Company Visits /signup
        ↓
Company Fills Form:
  - Company name
  - Domain
  - Admin email/credentials
        ↓
System Creates Tenant
        ↓
System Seeds Modules
        ↓
System Creates Admin Account
        ↓
Verification Email Sent
        ↓
Company Admin Verifies Email
        ↓
Company Admin Logs In
        ↓
Company Admin Creates Employees
```

**Pros**:
- Instant onboarding
- Scales automatically
- Better UX

**Cons**:
- Requires fraud detection
- Needs credit card verification
- Complex implementation

---

## 🚀 Implementation Needed: Enhanced Workflows

### Priority 1: Tenant Admin User Creation

**Problem**: Only SuperAdmin can create users, not tenant-level Admin.

**Solution**: Add endpoint for tenant Admin to create users:

```csharp
[HttpPost("create-user")]
[Authorize(Roles = "Admin,SuperAdmin")]
public async Task<ActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    // Admin can only create users in their own tenant
    var currentUserTenant = GetCurrentUserTenant();
    
    // Create user with tenant context
    var user = new User { /* ... */ };
    user.TenantId = currentUserTenant; // Automatically set
    
    // ... rest of creation logic
}
```

### Priority 2: Self-Service Company Signup

**Problem**: No public signup for new companies.

**Solution**: Add signup flow:

```csharp
[HttpPost("signup/company")]
[AllowAnonymous]
public async Task<ActionResult> CompanySignup([FromBody] CompanySignupDto dto)
{
    // 1. Create tenant
    var tenant = new Tenant
    {
        Name = dto.CompanyName,
        Domain = dto.Domain,
        IsActive = true
    };
    await _tenantService.CreateAsync(tenant);
    
    // 2. Seed modules
    await _moduleService.SeedForTenant(tenant.Id);
    
    // 3. Create admin user
    var admin = new User { /* ... */ };
    await _userManager.CreateAsync(admin, dto.AdminPassword);
    await _userManager.AddToRoleAsync(admin, "Admin");
    
    // 4. Send verification email
    await _emailService.SendVerificationAsync(admin.Email);
    
    return Ok(new { tenantId = tenant.Id, userId = admin.Id });
}
```

### Priority 3: Email Verification

**Problem**: No email verification workflow.

**Solution**: Implement ASP.NET Identity email confirmation:

```csharp
// In AuthService.RegisterAsync()
var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
var confirmationLink = $"https://yourapp.com/verify-email?token={token}&userId={user.Id}";

await _emailService.SendEmailAsync(
    user.Email,
    "Confirm Your Email",
    $"Click here to confirm: {confirmationLink}"
);

// Keep user inactive until verified
user.EmailConfirmed = false;
```

---

## 🔒 Security Considerations

### 1. Password Requirements
✅ **Implemented**:
- Minimum 12 characters
- Requires uppercase, lowercase, digit, special character
- 3 unique characters minimum

### 2. Account Lockout
⚠️ **Missing**: Add brute-force protection

```csharp
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
options.Lockout.MaxFailedAccessAttempts = 5;
```

### 3. Email Verification
⚠️ **Missing**: Prevent fake emails

Add email confirmation to registration flows.

### 4. Password Reset
✅ **Partially Implemented**: Admin can reset passwords

⚠️ **Missing**: User-initiated password reset

### 5. Multi-Factor Authentication (MFA)
⚠️ **Missing**: Consider adding TOTP authenticator app support

---

## 📊 Current Database Structure

### Tenant Isolation

```
Users Table:
- Id (PK)
- Email
- TenantId (nullable for platform admins)
- Roles (via AspNetUserRoles)

Tenants Table:
- Id (PK)
- Name (tenant identifier)
- Domain (optional)
- IsActive
```

### User-Tenant Association

**Platform Admin** (SuperAdmin):
- `TenantId = null`
- Can access all tenants

**Tenant Admin** (Admin):
- `TenantId = "acme-corp"`
- Only accesses their company data

**Employee**:
- `TenantId = "acme-corp"`
- Only accesses their company data

---

## 🧪 Testing Your Onboarding Flow

### Test Scenario 1: New Company Onboarding

```bash
# 1. Platform admin creates tenant
curl -X POST http://localhost:5192/api/tenants \
  -H "Authorization: Bearer <superadmin-jwt>" \
  -H "Content-Type: application/json" \
  -d '{"name":"testco","domain":"testco.com","isActive":true}'

# 2. Seed modules
curl -X POST http://localhost:5192/api/modules/seed \
  -H "Authorization: Bearer <superadmin-jwt>" \
  -H "X-Tenant-Id: testco"

# 3. Create admin user
curl -X POST http://localhost:5192/api/usermanagement/create-user \
  -H "Authorization: Bearer <superadmin-jwt>" \
  -H "Content-Type: application/json" \
  -d '{
    "email":"admin@testco.com",
    "password":"AdminPass123!@#",
    "firstName":"Test",
    "lastName":"Admin",
    "dateOfBirth":"1990-01-01",
    "role":"Admin"
  }'

# 4. Login as new admin
curl -X POST http://localhost:5192/api/auth/login \
  -H "X-Tenant-Id: testco" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@testco.com","password":"AdminPass123!@#"}'
```

### Test Scenario 2: Employee Self-Registration

```bash
curl -X POST http://localhost:5192/api/auth/register \
  -H "X-Tenant-Id: testco" \
  -H "Content-Type: application/json" \
  -d '{
    "email":"employee@testco.com",
    "password":"EmployeePass123!@#",
    "firstName":"Test",
    "lastName":"Employee",
    "dateOfBirth":"1995-06-15"
  }'
```

---

## 📚 Related Documentation

- **[User_Creation_Workflow.md](User_Creation_Workflow.md)** - Detailed technical implementation
- **[TENANT_CREATION_GUIDE.md](TENANT_CREATION_GUIDE.md)** - Tenant setup instructions
- **[LOGIN_CREDENTIALS.md](Frontend/LOGIN_CREDENTIALS.md)** - Default credentials
- **[SESSION_MANAGEMENT_GUIDE.md](SESSION_MANAGEMENT_GUIDE.md)** - Authentication details

---

## 🎉 Summary

**Current State**: SmallHR supports multi-tenant onboarding with manual platform admin setup.

**Three Ways to Create Logins**:
1. ✅ **Public Registration** - Employees self-register
2. ✅ **Admin Creation** - SuperAdmin creates accounts
3. ⚠️ **Tenant Admin Creation** - Needs implementation

**Recommended Next Steps**:
1. Add email verification
2. Implement tenant-level Admin user creation
3. Build self-service company signup flow
4. Add account lockout protection
5. Implement user-initiated password reset

