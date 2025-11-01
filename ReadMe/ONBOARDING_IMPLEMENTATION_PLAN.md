# Company Onboarding Enhancement Implementation Plan

## Overview
This document outlines the implementation plan for enhancing the company onboarding and user creation workflows based on the recommendations in `COMPANY_ONBOARDING_GUIDE.md`.

---

## 🎯 Implementation Phases

### **Phase 1: Critical Security Enhancements** (Week 1)
**Priority**: High | **Impact**: Security | **Complexity**: Low-Medium

#### 1.1 Account Lockout Protection
**Why**: Prevent brute-force login attacks

**Implementation**:
```csharp
// In Program.cs - AddIdentity configuration
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // ... existing password options ...
    
    // Lockout settings
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
});
```

**Files to modify**:
- `SmallHR.API/Program.cs`
- `SmallHR.API/Controllers/AuthController.cs` - Update error messages

**Testing**:
- Attempt 5+ failed logins
- Verify account locks for 15 minutes
- Verify lockout clears after time expires

---

#### 1.2 Email Verification Workflow
**Why**: Prevent fake email accounts and improve security

**Backend Implementation**:

**Step 1**: Add EmailService
```csharp
// SmallHR.Core/Interfaces/IEmailService.cs
public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string token, string userId);
    Task SendPasswordResetEmailAsync(string email, string token);
    Task SendWelcomeEmailAsync(string email, string firstName);
}
```

**Step 2**: Implement EmailService
```csharp
// SmallHR.API/Services/EmailService.cs
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    
    // Implement SendGrid, SMTP, or other email provider
    public async Task SendVerificationEmailAsync(string email, string token, string userId)
    {
        var verificationLink = $"{_configuration["AppSettings:BaseUrl"]}/verify-email?token={token}&userId={userId}";
        
        // TODO: Send email via SendGrid, SMTP, etc.
        _logger.LogInformation("Verification email would be sent to {Email}", email);
    }
    
    // ... other methods
}
```

**Step 3**: Update AuthService
```csharp
// In AuthService.RegisterAsync()
public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
{
    var user = new User { /* ... */ };
    
    var result = await _userManager.CreateAsync(user, registerDto.Password);
    if (!result.Succeeded) { /* ... */ }
    
    // Generate email confirmation token
    var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
    
    // Send verification email
    await _emailService.SendVerificationEmailAsync(user.Email, confirmationToken, user.Id);
    
    // Keep user inactive until verified
    user.EmailConfirmed = false;
    await _userManager.UpdateAsync(user);
    
    // Don't generate JWT until email verified
    throw new InvalidOperationException("Please verify your email before logging in");
}
```

**Step 4**: Add Email Verification Endpoint
```csharp
// In AuthController
[HttpPost("verify-email")]
[AllowAnonymous]
public async Task<ActionResult> VerifyEmail([FromQuery] string token, [FromQuery] string userId)
{
    var user = await _userManager.FindByIdAsync(userId);
    if (user == null)
        return BadRequest(new { message = "Invalid user" });
    
    var result = await _userManager.ConfirmEmailAsync(user, token);
    if (result.Succeeded)
    {
        // Auto-login after verification
        var authResult = await _authService.LoginAsync(new LoginDto 
        { 
            Email = user.Email, 
            Password = /* from temp storage or separate flow */ 
        });
        
        SetAuthCookies(authResult.Token, authResult.RefreshToken);
        return Ok(new { message = "Email verified successfully", user = authResult.User });
    }
    
    return BadRequest(new { message = "Invalid verification token" });
}
```

**Step 5**: Update Login to check EmailConfirmed
```csharp
// In AuthService.LoginAsync()
var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
if (!result.Succeeded)
    throw new UnauthorizedAccessException("Invalid credentials");

// Check if email is verified
if (!user.EmailConfirmed)
    throw new UnauthorizedAccessException("Please verify your email before logging in");
```

**Frontend Implementation**:

**Step 1**: Create Verification Page
```typescript
// SmallHR.Web/src/pages/VerifyEmail.tsx
import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Result, Button, Spin } from 'antd';

export default function VerifyEmail() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  
  useEffect(() => {
    const token = searchParams.get('token');
    const userId = searchParams.get('userId');
    
    if (!token || !userId) {
      setStatus('error');
      return;
    }
    
    // Call verification endpoint
    api.post(`/api/auth/verify-email?token=${token}&userId=${userId}`)
      .then(() => setStatus('success'))
      .catch(() => setStatus('error'));
  }, [searchParams]);
  
  if (status === 'loading') return <Spin size="large" />;
  if (status === 'error') return <Result status="error" title="Verification Failed" />;
  
  return <Result status="success" title="Email Verified!" />;
}
```

**Step 2**: Update Registration Flow
- Show "Check your email" message after registration
- Redirect to `/verify-email` page
- Add resend verification email functionality

**Files to create**:
- `SmallHR.Core/Interfaces/IEmailService.cs`
- `SmallHR.API/Services/EmailService.cs`
- `SmallHR.Web/src/pages/VerifyEmail.tsx`

**Files to modify**:
- `SmallHR.API/Services/AuthService.cs`
- `SmallHR.API/Controllers/AuthController.cs`
- `SmallHR.Web/src/pages/Login.tsx`
- `SmallHR.Web/src/App.tsx` - Add route

**Dependencies to add**:
```xml
<!-- SendGrid or other email provider -->
<PackageReference Include="SendGrid" Version="9.29.3" />
```

**Testing**:
- Register new user
- Verify email sent (check logs)
- Click verification link
- Attempt login before verification
- Attempt login after verification

---

#### 1.3 User-Initiated Password Reset
**Why**: Allow users to reset forgotten passwords without admin intervention

**Backend Implementation**:

**Step 1**: Add Forgot Password Endpoint
```csharp
// In AuthController
[HttpPost("forgot-password")]
[AllowAnonymous]
public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
{
    var user = await _userManager.FindByEmailAsync(dto.Email);
    if (user == null)
    {
        // Don't reveal if email exists (security best practice)
        return Ok(new { message = "If email exists, reset instructions sent" });
    }
    
    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    await _emailService.SendPasswordResetEmailAsync(user.Email, token);
    
    return Ok(new { message = "Password reset instructions sent to your email" });
}
```

**Step 2**: Add Reset Password Endpoint
```csharp
[HttpPost("reset-password")]
[AllowAnonymous]
public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
{
    var user = await _userManager.FindByEmailAsync(dto.Email);
    if (user == null)
        return BadRequest(new { message = "Invalid request" });
    
    var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
    if (!result.Succeeded)
        return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
    
    return Ok(new { message = "Password reset successfully" });
}
```

**Frontend Implementation**:

**Step 1**: Create ForgotPassword Page
```typescript
// SmallHR.Web/src/pages/ForgotPassword.tsx
export default function ForgotPassword() {
  const onFinish = async (values: { email: string }) => {
    await api.post('/api/auth/forgot-password', values);
    message.success('Check your email for reset instructions');
  };
  
  return (
    <Form onFinish={onFinish}>
      <Form.Item name="email" rules={[{ required: true, type: 'email' }]}>
        <Input placeholder="Enter your email" />
      </Form.Item>
      <Button type="primary" htmlType="submit">Send Reset Link</Button>
    </Form>
  );
}
```

**Step 2**: Create ResetPassword Page
```typescript
// SmallHR.Web/src/pages/ResetPassword.tsx
export default function ResetPassword() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const email = searchParams.get('email');
  
  const onFinish = async (values: { newPassword: string }) => {
    await api.post('/api/auth/reset-password', {
      email,
      token,
      newPassword: values.newPassword
    });
    message.success('Password reset successfully');
  };
  
  // ... form with new password field
}
```

**Files to create**:
- `SmallHR.Web/src/pages/ForgotPassword.tsx`
- `SmallHR.Web/src/pages/ResetPassword.tsx`

**Files to modify**:
- `SmallHR.API/Controllers/AuthController.cs`
- `SmallHR.Core/DTOs/Auth/AuthDto.cs` - Add DTOs
- `SmallHR.Web/src/App.tsx` - Add routes
- `SmallHR.Web/src/pages/Login.tsx` - Add "Forgot Password" link

**Testing**:
- Request password reset
- Check email for reset link
- Use reset link to set new password
- Attempt to use old password (should fail)
- Attempt to use reset link twice (should fail)

---

### **Phase 2: Enhanced User Management** (Week 2)
**Priority**: Medium | **Impact**: Functionality | **Complexity**: Medium

#### 2.1 Tenant-Level Admin User Creation
**Why**: Allow company admins to create users without requiring platform SuperAdmin

**Implementation**:

**Step 1**: Extract User Creation Logic to Service
```csharp
// SmallHR.Core/Interfaces/IUserManagementService.cs
public interface IUserManagementService
{
    Task<CreateUserResult> CreateUserAsync(CreateUserDto dto, string? tenantId = null);
    Task<bool> UpdateUserRoleAsync(string userId, string role);
    Task<bool> ToggleUserStatusAsync(string userId);
    Task<bool> ResetUserPasswordAsync(string userId, string newPassword);
}

// SmallHR.API/Services/UserManagementService.cs
public class UserManagementService : IUserManagementService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITenantProvider _tenantProvider;
    
    public async Task<CreateUserResult> CreateUserAsync(CreateUserDto dto, string? tenantId = null)
    {
        // Determine tenant: use provided tenantId or current user's tenant
        var effectiveTenantId = tenantId ?? _tenantProvider.TenantId;
        
        // Check if user exists
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return CreateUserResult.Failure("User already exists");
        
        // Validate role exists
        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return CreateUserResult.Failure("Invalid role");
        
        // Create user
        var user = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            IsActive = true
        };
        
        // Set tenant if provided (for multi-tenant scenarios)
        // Note: User entity may need TenantId property
        
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return CreateUserResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        
        // Assign role
        await _userManager.AddToRoleAsync(user, dto.Role);
        
        return CreateUserResult.Success(user.Id, user.Email);
    }
    
    // ... other methods
}
```

**Step 2**: Update UserManagementController
```csharp
// In UserManagementController
private readonly IUserManagementService _userManagementService;

[HttpPost("create-user")]
[Authorize(Roles = "Admin,SuperAdmin")]
public async Task<ActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    var currentUser = await _userManager.GetUserAsync(User);
    var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
    
    // If user is Admin (not SuperAdmin), ensure they can only create users in their tenant
    if (currentUserRoles.Contains("Admin") && !currentUserRoles.Contains("SuperAdmin"))
    {
        // Admin can only create users in their own tenant
        // Tenant isolation will be enforced by the service
    }
    
    var result = await _userManagementService.CreateUserAsync(new CreateUserDto
    {
        Email = request.Email,
        Password = request.Password,
        FirstName = request.FirstName,
        LastName = request.LastName,
        DateOfBirth = request.DateOfBirth,
        Role = request.Role
    });
    
    if (!result.Success)
        return BadRequest(new { message = result.ErrorMessage });
    
    return Ok(new { 
        message = "User created successfully",
        userId = result.UserId,
        email = result.Email,
        role = request.Role
    });
}
```

**Files to create**:
- `SmallHR.Core/Interfaces/IUserManagementService.cs`
- `SmallHR.API/Services/UserManagementService.cs`

**Files to modify**:
- `SmallHR.API/Controllers/UserManagementController.cs`
- `SmallHR.API/Program.cs` - Register service

**Testing**:
- Login as tenant Admin
- Create user in same tenant (should succeed)
- Attempt to create user in different tenant (should fail)
- Login as SuperAdmin
- Create user in any tenant (should succeed)

---

### **Phase 3: Self-Service Company Signup** (Week 3-4)
**Priority**: Low | **Impact**: UX | **Complexity**: High

#### 3.1 Company Signup Backend
**Implementation**:

**Step 1**: Create CompanySignupDto
```csharp
// SmallHR.Core/DTOs/Auth/AuthDto.cs
public class CompanySignupDto
{
    [Required]
    public string CompanyName { get; set; } = string.Empty;
    
    [Required]
    public string Domain { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;
    
    [Required]
    [MinLength(12)]
    public string AdminPassword { get; set; } = string.Empty;
    
    [Required]
    public string AdminFirstName { get; set; } = string.Empty;
    
    [Required]
    public string AdminLastName { get; set; } = string.Empty;
    
    [Required]
    public DateTime AdminDateOfBirth { get; set; }
}
```

**Step 2**: Create Company Signup Endpoint
```csharp
// In AuthController
[HttpPost("signup/company")]
[AllowAnonymous]
public async Task<ActionResult> CompanySignup([FromBody] CompanySignupDto dto)
{
    try
    {
        // 1. Create tenant
        var tenant = new Tenant
        {
            Name = dto.CompanyName.ToLower().Replace(" ", "-"),
            Domain = dto.Domain,
            IsActive = true
        };
        
        await _tenantService.CreateAsync(tenant);
        
        // 2. Seed modules for tenant
        await _moduleService.SeedForTenant(tenant.Id);
        
        // 3. Create admin user
        var admin = new User
        {
            UserName = dto.AdminEmail,
            Email = dto.AdminEmail,
            FirstName = dto.AdminFirstName,
            LastName = dto.AdminLastName,
            DateOfBirth = dto.AdminDateOfBirth,
            IsActive = true
        };
        
        var result = await _userManager.CreateAsync(admin, dto.AdminPassword);
        if (!result.Succeeded)
        {
            // Rollback tenant if user creation fails
            await _tenantService.DeleteAsync(tenant.Id);
            return BadRequest(new { message = "User creation failed: " + string.Join(", ", result.Errors.Select(e => e.Description)) });
        }
        
        await _userManager.AddToRoleAsync(admin, "Admin");
        
        // 4. Send verification email
        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(admin);
        await _emailService.SendVerificationEmailAsync(admin.Email, confirmationToken, admin.Id);
        
        // 5. Send welcome email
        await _emailService.SendWelcomeEmailAsync(admin.Email, admin.FirstName);
        
        return Ok(new { 
            message = "Company account created successfully. Please verify your email.",
            tenantId = tenant.Id,
            userId = admin.Id
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during company signup");
        return StatusCode(500, new { message = "An error occurred during company signup" });
    }
}
```

**Step 3**: Add Tenant/Module Services
```csharp
// Extend existing service or create new ones
public interface ITenantService
{
    Task<Tenant> CreateAsync(Tenant tenant);
    Task<bool> DeleteAsync(string tenantId);
}

public interface IModuleService
{
    Task SeedForTenant(string tenantId);
}
```

**Files to create**:
- `SmallHR.Web/src/pages/CompanySignup.tsx`

**Files to modify**:
- `SmallHR.API/Controllers/AuthController.cs`
- `SmallHR.Core/DTOs/Auth/AuthDto.cs`
- `SmallHR.Web/src/App.tsx`

**Testing**:
- Create company via signup form
- Verify tenant created
- Verify modules seeded
- Verify admin user created
- Verify verification email sent
- Attempt duplicate company name (should fail)

---

### **Phase 4: Additional Enhancements** (Week 5)
**Priority**: Low | **Impact**: Security/Compliance | **Complexity**: Medium-High

#### 4.1 Password History Tracking
**Implementation**: Create `PasswordHistory` table and track previous passwords

#### 4.2 Audit Trail
**Implementation**: Create `AuditLog` table and log user creation/deletion events

#### 4.3 Multi-Factor Authentication
**Implementation**: Add TOTP authenticator support using ASP.NET Identity

---

## 📋 Implementation Checklist

### Backend Tasks
- [ ] Configure account lockout in `Program.cs`
- [ ] Create `IEmailService` interface
- [ ] Implement `EmailService` with SendGrid/SMTP
- [ ] Add email verification to `AuthService.RegisterAsync()`
- [ ] Add `VerifyEmail` endpoint to `AuthController`
- [ ] Add `ForgotPassword` endpoint to `AuthController`
- [ ] Add `ResetPassword` endpoint to `AuthController`
- [ ] Create `IUserManagementService` interface
- [ ] Implement `UserManagementService`
- [ ] Update `UserManagementController` to use service
- [ ] Add `CompanySignup` endpoint to `AuthController`
- [ ] Create transaction management for atomic operations
- [ ] Add password history tracking
- [ ] Add audit logging for user events

### Frontend Tasks
- [ ] Create `VerifyEmail.tsx` page
- [ ] Create `ForgotPassword.tsx` page
- [ ] Create `ResetPassword.tsx` page
- [ ] Create `CompanySignup.tsx` page
- [ ] Update `Login.tsx` with "Forgot Password" link
- [ ] Update registration flow to show email verification message
- [ ] Add email resend functionality
- [ ] Add routes to `App.tsx`
- [ ] Update user creation forms for tenant admin

### Configuration
- [ ] Configure SendGrid API key in `appsettings.json`
- [ ] Add email templates
- [ ] Configure base URL for email links
- [ ] Set up email service in `Program.cs`

### Testing
- [ ] Unit tests for email service
- [ ] Unit tests for user management service
- [ ] Integration tests for registration flow
- [ ] Integration tests for password reset flow
- [ ] Integration tests for company signup
- [ ] Security tests for tenant isolation
- [ ] End-to-end tests for complete workflows

---

## 🔗 Dependencies

### NuGet Packages
```xml
<PackageReference Include="SendGrid" Version="9.29.3" />
<PackageReference Include="Microsoft.AspNetCore.Identity.UI" Version="8.0.0" />
```

### npm Packages
```json
{
  "dependencies": {
    // No additional packages needed, using existing Ant Design components
  }
}
```

---

## 📝 Notes

### Email Service Options
1. **SendGrid** (Recommended) - Easy integration, generous free tier
2. **SMTP** - More control, requires email server
3. **AWS SES** - Cost-effective at scale
4. **Azure Communication Services** - Good if using Azure

### Security Considerations
- Rate limit email sending endpoints
- Use secure token generation
- Expire tokens after reasonable time (1 hour for reset, 24 hours for verification)
- Don't reveal if email exists during password reset
- Log all security events

### Rollback Strategy
- If company signup fails, rollback tenant creation
- Use database transactions for atomicity
- Implement cleanup jobs for orphaned records

---

## 🎯 Success Metrics

After implementation, measure:
- User registration success rate
- Email verification completion rate
- Password reset success rate
- Account lockout incidents
- Support tickets for login issues
- Company onboarding time
- User satisfaction scores

---

## 📚 Related Documentation
- [COMPANY_ONBOARDING_GUIDE.md](COMPANY_ONBOARDING_GUIDE.md) - Overview of workflows
- [User_Creation_Workflow.md](User_Creation_Workflow.md) - Technical details
- [TENANT_CREATION_GUIDE.md](TENANT_CREATION_GUIDE.md) - Tenant setup

