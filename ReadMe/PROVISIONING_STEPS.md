# Tenant Provisioning Steps Reference

This file tracks all provisioning steps executed when setting up a new tenant. Update this file as you add new features that require tenant-specific initialization.

---

## Current Provisioning Steps

### ✅ Step 1: Subscription Creation
**Status**: Implemented  
**Location**: `TenantProvisioningService.cs` - Line ~97-144  
**Purpose**: Creates a subscription for the tenant (defaults to Free plan)  
**Details**:
- Checks if subscription already exists (idempotent)
- Defaults to Free plan if no plan specified
- Supports trial start if plan supports it
- Links subscription to tenant
- Sets billing period (defaults to Monthly)

**When to Update**: Add new subscription plans or billing options

---

### ✅ Step 2: Roles Verification
**Status**: Implemented  
**Location**: `TenantProvisioningService.cs` - Line ~93-95, ~210-225  
**Purpose**: Ensures all required roles exist globally  
**Current Roles**:
- `SuperAdmin` - Full system access
- `Admin` - Tenant admin access
- `HR` - HR manager access
- `Employee` - Basic employee access

**When to Update**: 
- Add new roles (e.g., "Manager", "Auditor", "Viewer")
- Update role permissions

**Example**:
```csharp
// Add to EnsureRolesExistAsync method
if (!await roleManager.RoleExistsAsync("Manager"))
{
    await roleManager.CreateAsync(new IdentityRole("Manager"));
}
```

---

### ✅ Step 3: Modules Seeding
**Status**: Implemented  
**Location**: `TenantProvisioningService.cs` - Line ~146-181  
**Purpose**: Creates tenant-specific navigation modules  
**Current Modules**:
- `/dashboard` - Main dashboard
- `/employees` - Employee management
- `/organization` - Organization structure
- `/departments` - Department management
- `/positions` - Position management

**When to Update**: 
- Add new modules (e.g., `/reports`, `/analytics`, `/settings`)
- Update module hierarchy
- Add new module properties

**Example**:
```csharp
// Add to SeedTenantModulesAsync method
new { 
    Path = "/reports", 
    Name = "Reports", 
    ParentPath = (string?)null, 
    Icon = "FileTextOutlined", 
    DisplayOrder = 4, 
    Description = "Reports and analytics" 
},
```

---

### ✅ Step 4: Departments & Positions Seeding
**Status**: Implemented  
**Location**: `TenantProvisioningService.cs` - Line ~183-257  
**Purpose**: Creates default organizational structure  
**Current Departments**:
- People/HR
- Engineering
- Sales
- Finance
- Customer Support
- Operations

**Current Positions**: ~15 default positions across departments

**When to Update**:
- Add new default departments
- Add new default positions
- Update department/position structure

**Example**:
```csharp
// Add to SeedTenantDepartmentsAndPositionsAsync method
var departmentNames = new[] { 
    "People/HR", 
    "Engineering", 
    "Sales",
    "Finance",
    "Customer Support",
    "Operations",
    "Marketing",  // NEW
    "Legal"       // NEW
};
```

---

### ✅ Step 5: Role Permissions Seeding
**Status**: Implemented  
**Location**: `TenantProvisioningService.cs` - Line ~259-322  
**Purpose**: Sets up role-based access control for all pages  
**Current Permissions**:
- Dashboard (all roles)
- Employees (all roles, different access levels)
- Departments (all roles, different access levels)
- Positions (all roles, different access levels)
- Organization (all roles, different access levels)

**When to Update**:
- Add new pages/routes
- Update permission rules for existing pages
- Add new permission types (e.g., Export, Import)

**Example**:
```csharp
// Add to SeedTenantRolePermissionsAsync method
var pages = new[]
{
    new { Path = "/dashboard", Name = "Dashboard", Description = "Main dashboard page" },
    new { Path = "/employees", Name = "Employees", Description = "Employee management" },
    new { Path = "/reports", Name = "Reports", Description = "Reports and analytics" },  // NEW
    // ... existing pages
};
```

---

### ✅ Step 6: Admin User Creation
**Status**: Implemented  
**Location**: `TenantProvisioningService.cs` - Line ~324-369  
**Purpose**: Creates tenant admin user account  
**Details**:
- Creates user with provided email
- Generates secure temporary password
- Creates password reset token
- Assigns user to tenant

**When to Update**:
- Add user properties (e.g., phone, department assignment)
- Update user creation requirements
- Add user validation rules

**Example**:
```csharp
// Add to CreateTenantAdminUserAsync method
var user = new User
{
    UserName = email,
    Email = email,
    FirstName = firstName,
    LastName = lastName,
    PhoneNumber = request.PhoneNumber,  // NEW
    // ... existing properties
};
```

---

### ✅ Step 7: Admin Role Assignment
**Status**: Implemented  
**Location**: `TenantProvisioningService.cs` - Line ~175-180  
**Purpose**: Assigns Admin role to tenant admin user  
**Details**:
- Checks if role already assigned (idempotent)
- Assigns Admin role
- Enables tenant management access

**When to Update**:
- Assign additional roles (e.g., SuperAdmin for platform admins)
- Add role assignment conditions

**Example**:
```csharp
// Add to provisioning service
if (request.AssignSuperAdminRole)
{
    await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
}
```

---

### ✅ Step 8: Welcome Email
**Status**: Implemented  
**Location**: `TenantProvisioningService.cs` - Line ~182-198  
**Purpose**: Sends welcome email with login details  
**Details**:
- Email sent to admin user
- Includes password setup link
- Contains tenant name and welcome message
- Uses password reset token

**When to Update**:
- Update email template
- Add additional information to email
- Add email notifications for other users

**Example**:
```csharp
// Add to provisioning service
await _emailService.SendTenantAdminInviteEmailAsync(
    adminEmail,
    adminFirstName,
    tenant.Name,
    passwordToken,
    adminUser.Id,
    subscriptionInfo  // NEW
);
```

---

## Future Provisioning Steps (Not Yet Implemented)

### 📋 Step 9: Default Settings Configuration
**Status**: Planned  
**Purpose**: Create tenant-specific settings  
**Suggested Implementation**:
- Company branding (logo, colors)
- Timezone settings
- Locale/currency settings
- Notification preferences
- Feature toggles

**Location to Add**: After Step 8 (Welcome Email)

**Example**:
```csharp
// Add new method SeedTenantSettingsAsync
private async Task SeedTenantSettingsAsync(ApplicationDbContext context, string tenantId)
{
    var settings = new TenantSettings
    {
        TenantId = tenantId,
        Timezone = "UTC",
        Currency = "USD",
        Locale = "en-US",
        CompanyLogo = null,
        BrandColor = "#1890ff"
    };
    
    await context.TenantSettings.AddAsync(settings);
    await context.SaveChangesAsync();
}
```

---

### 📋 Step 10: Workflow Templates
**Status**: Planned  
**Purpose**: Create default workflow templates  
**Suggested Implementation**:
- Leave request workflow
- Approval workflows
- Onboarding workflow
- Offboarding workflow

**Location to Add**: After Step 9 (Default Settings)

---

### 📋 Step 11: Notification Templates
**Status**: Planned  
**Purpose**: Create default notification templates  
**Suggested Implementation**:
- Email templates for various events
- SMS templates
- Push notification templates

**Location to Add**: After Step 10 (Workflow Templates)

---

### 📋 Step 12: Integration Setup
**Status**: Planned  
**Purpose**: Configure default integrations  
**Suggested Implementation**:
- Calendar integration settings
- Email integration settings
- Third-party API connections
- Webhook endpoints

**Location to Add**: After Step 11 (Notification Templates)

---

### 📋 Step 13: Reports & Dashboards
**Status**: Planned  
**Purpose**: Create default reports and dashboards  
**Suggested Implementation**:
- Employee summary dashboard
- Leave analytics report
- Attendance reports
- Custom report templates

**Location to Add**: After Step 12 (Integration Setup)

---

### 📋 Step 14: Data Retention Policies
**Status**: Planned  
**Purpose**: Set default data retention policies  
**Suggested Implementation**:
- Backup schedules
- Data archival rules
- GDPR compliance settings
- Data deletion policies

**Location to Add**: After Step 13 (Reports & Dashboards)

---

## How to Add New Provisioning Steps

### Step-by-Step Guide

1. **Add Entity** (if needed)
   - Create entity in `SmallHR.Core/Entities/`
   - Add to `ApplicationDbContext`
   - Create migration

2. **Create Seed Method**
   ```csharp
   private async Task SeedYourNewFeatureAsync(ApplicationDbContext context, string tenantId)
   {
       // Your seeding logic here
       // Ensure idempotent (check if exists first)
   }
   ```

3. **Add to Provisioning Service**
   ```csharp
   // In ProvisionTenantAsync method, add after appropriate step:
   
   // Step X: Seed Your New Feature
   await SeedYourNewFeatureAsync(tenantCtx, tenantIdString);
   result.StepsCompleted.Add("Your feature seeded");
   ```

4. **Update This File**
   - Add new step to "Current Provisioning Steps" section
   - Include status, location, purpose, and update instructions

5. **Test**
   - Test provisioning with new tenant
   - Verify step executes correctly
   - Ensure idempotency (safe to run multiple times)

---

## Provisioning Step Checklist

When adding a new provisioning step, ensure:

- ✅ **Idempotent** - Can run multiple times safely
- ✅ **Tenant-Scoped** - Data is tenant-specific (uses `TenantId`)
- ✅ **Error Handling** - Graceful failure (doesn't block other steps)
- ✅ **Logged** - Steps are logged for monitoring
- ✅ **Tracked** - Added to `StepsCompleted` in result
- ✅ **Tested** - Verified in development environment
- ✅ **Documented** - Added to this file

---

## Provisioning Order Guidelines

**Order Matters!** Follow this dependency order:

1. **Roles** - Must be first (needed for permissions)
2. **Subscription** - Needed for feature access checks
3. **Base Data** - Modules, Departments, Positions (needed for other features)
4. **Permissions** - Depends on Roles and Modules
5. **Users** - Can be created anytime (no dependencies)
6. **Settings** - Can be created anytime
7. **Derived Data** - Reports, Dashboards (depend on base data)
8. **Integrations** - Can be last (optional features)

---

## Provisioning Service Method Structure

### Current Method Pattern

```csharp
private async Task SeedYourFeatureAsync(ApplicationDbContext context, string tenantId)
{
    // 1. Define default data
    var items = new[] { /* default items */ };
    
    // 2. Check existing (idempotent)
    foreach (var item in items)
    {
        var existing = await context.YourFeature
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && /* match condition */);
        
        // 3. Create if not exists
        if (existing == null)
        {
            var newItem = new YourFeature
            {
                TenantId = tenantId,
                // ... properties
            };
            await context.YourFeature.AddAsync(newItem);
        }
    }
    
    // 4. Save changes
    await context.SaveChangesAsync();
}
```

---

## Example: Adding a New Step

### Scenario: Add "Default Holiday Calendar"

**1. Create Entity** (if needed):
```csharp
// SmallHR.Core/Entities/HolidayCalendar.cs
public class HolidayCalendar : BaseEntity
{
    public required string TenantId { get; set; }
    public required string Name { get; set; }
    public DateTime HolidayDate { get; set; }
    public string? Description { get; set; }
}
```

**2. Create Seed Method**:
```csharp
private async Task SeedHolidayCalendarAsync(ApplicationDbContext context, string tenantId)
{
    var defaultHolidays = new[]
    {
        new { Name = "New Year's Day", Date = new DateTime(DateTime.Now.Year, 1, 1) },
        new { Name = "Independence Day", Date = new DateTime(DateTime.Now.Year, 7, 4) },
        // ... more holidays
    };
    
    foreach (var holiday in defaultHolidays)
    {
        var existing = await context.HolidayCalendars
            .FirstOrDefaultAsync(h => h.TenantId == tenantId && 
                                     h.HolidayDate == holiday.Date);
        
        if (existing == null)
        {
            await context.HolidayCalendars.AddAsync(new HolidayCalendar
            {
                TenantId = tenantId,
                Name = holiday.Name,
                HolidayDate = holiday.Date,
                Description = holiday.Name
            });
        }
    }
    
    await context.SaveChangesAsync();
}
```

**3. Add to Provisioning**:
```csharp
// In ProvisionTenantAsync, after Step 4 (Departments & Positions):

// Step 4.5: Seed Holiday Calendar
await SeedHolidayCalendarAsync(tenantCtx, tenantIdString);
result.StepsCompleted.Add("Holiday calendar seeded");
```

**4. Update This File**:
```markdown
### ✅ Step 4.5: Holiday Calendar Seeding
**Status**: Implemented  
**Location**: `TenantProvisioningService.cs` - Line ~XXX  
**Purpose**: Creates default holiday calendar for tenant  
...
```

---

## Testing New Provisioning Steps

### Test Checklist

1. **Create New Tenant**
   ```bash
   POST /api/tenants
   ```

2. **Verify Provisioning**
   ```bash
   GET /api/tenants/{id}/status
   ```

3. **Check Provisioning Result**
   ```bash
   POST /api/provisioning/{id}
   # Verify new step in StepsCompleted
   ```

4. **Test Idempotency**
   - Run provisioning multiple times
   - Verify no duplicate data created

5. **Test Failure Handling**
   - Simulate failure scenario
   - Verify graceful failure
   - Verify other steps still complete

---

## Provisioning Status Tracking

### Current Status Values

- `Provisioning` - Provisioning in progress
- `Active` - Provisioning completed successfully
- `Failed` - Provisioning failed (check `failureReason`)

### Steps Completed Tracking

Each provisioning step is tracked in `result.StepsCompleted`:
- ✅ Successfully completed steps
- ❌ Failed steps (with error message)
- ⏭️ Skipped steps (with reason)

---

## Maintenance Notes

### Last Updated
- **Date**: 2025-11-02
- **Version**: 1.0
- **Total Steps**: 8

### Recent Changes
- ✅ Added subscription creation step
- ✅ Added detailed step tracking
- ✅ Added provisioning result structure

### Next Review
- Review quarterly or when adding major features
- Update "Future Provisioning Steps" section
- Move planned steps to implemented as they're added

---

## Quick Reference

### Provisioning Service File
- **File**: `SmallHR.Infrastructure/Services/TenantProvisioningService.cs`
- **Interface**: `SmallHR.Core/Interfaces/ITenantProvisioningService.cs`
- **Controller**: `SmallHR.API/Controllers/TenantProvisioningController.cs`
- **Background Worker**: `SmallHR.API/HostedServices/TenantProvisioningHostedService.cs`

### Key Methods
- `ProvisionTenantAsync()` - Main provisioning method
- `EnsureRolesExistAsync()` - Roles verification
- `SeedTenantModulesAsync()` - Modules seeding
- `SeedTenantDepartmentsAndPositionsAsync()` - Departments/Positions
- `SeedTenantRolePermissionsAsync()` - Permissions seeding
- `CreateTenantAdminUserAsync()` - Admin user creation

---

**Remember**: Always update this file when adding new provisioning steps! 📝

