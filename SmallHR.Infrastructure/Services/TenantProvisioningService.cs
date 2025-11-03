using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Services;

public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantProvisioningService> _logger;
    private readonly IConnectionResolver _connectionResolver;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public TenantProvisioningService(
        IServiceProvider serviceProvider,
        ILogger<TenantProvisioningService> logger,
        IConnectionResolver connectionResolver,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _connectionResolver = connectionResolver;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<(bool Success, string? ErrorMessage, ITenantProvisioningService.ProvisioningResult? Result)> ProvisionTenantAsync(
        int tenantId, 
        string adminEmail, 
        string adminFirstName, 
        string adminLastName,
        int? subscriptionPlanId = null,
        bool startTrial = false)
    {
        try
        {
            _logger.LogInformation("Starting provisioning for tenant {TenantId}", tenantId);

            // Create a scope for this operation to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var resolver = scope.ServiceProvider.GetRequiredService<IConnectionResolver>();
            
            // Get connection string for default/master database where Tenants table exists
            var masterConn = resolver.GetConnectionString("default");
            var masterOpts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(masterConn)
                .Options;
            
            // Create tenant provider for master DB
            var masterTenantProvider = new MasterTenantProvider("default");
            
            // Get tenant record
            await using var masterCtx = new ApplicationDbContext(masterOpts, masterTenantProvider);
            var tenant = await masterCtx.Tenants.FindAsync(tenantId);
            
            if (tenant == null)
            {
                _logger.LogError("Tenant {TenantId} not found", tenantId);
                return (false, "Tenant not found", null);
            }

            // Use tenant ID (int) as string for consistency - not tenant name
            // This ensures departments/positions seeded during provisioning use the same TenantId format
            // as departments/positions created manually via API (which use _tenantProvider.TenantId)
            var tenantIdString = tenantId.ToString();
            
            // Get connection string for tenant (for shared DB, this will be the same as master)
            // Connection string resolver can handle both tenant ID and tenant name
            var tenantConn = resolver.GetConnectionString(tenantIdString);
            var tenantOpts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(tenantConn)
                .Options;
            
            // Create tenant provider for tenant-specific operations
            // Use tenant ID (int) as string for consistency across the system
            var tenantProvider = new ProvisioningTenantProvider(tenantIdString);
            
            await using var tenantCtx = new ApplicationDbContext(tenantOpts, tenantProvider);
            
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

            var result = new ITenantProvisioningService.ProvisioningResult
            {
                TenantId = tenantId,
                TenantName = tenant.Name ?? string.Empty,
                StepsCompleted = new List<string>()
            };

            // Step 1: Ensure roles exist (roles are global, not tenant-specific)
            await EnsureRolesExistAsync(roleManager);
            result.StepsCompleted.Add("Roles created/verified");

            // Step 2: Create subscription (if not exists)
            int? subscriptionId = null;
            try
            {
                var existingSubscription = await subscriptionService.GetSubscriptionByTenantIdAsync(tenantId);
                if (existingSubscription == null)
                {
                    // Get subscription plan (default to Free plan if not specified)
                    if (!subscriptionPlanId.HasValue)
                    {
                        var freePlan = await subscriptionService.GetPlanByNameAsync("Free");
                        if (freePlan != null)
                            subscriptionPlanId = freePlan.Id;
                    }

                    if (subscriptionPlanId.HasValue)
                    {
                        var createSubscriptionRequest = new SmallHR.Core.DTOs.Subscription.CreateSubscriptionRequest
                        {
                            TenantId = tenantId,
                            SubscriptionPlanId = subscriptionPlanId.Value,
                            BillingPeriod = SmallHR.Core.Entities.BillingPeriod.Monthly,
                            StartTrial = startTrial
                        };

                        var subscription = await subscriptionService.CreateSubscriptionAsync(createSubscriptionRequest);
                        subscriptionId = subscription.Id;
                        result.SubscriptionId = subscription.Id;
                        result.StepsCompleted.Add($"Subscription created (Plan ID: {subscriptionPlanId.Value})");
                    }
                    else
                    {
                        _logger.LogWarning("No subscription plan found. Skipping subscription creation.");
                        result.StepsCompleted.Add("Subscription skipped (no plan found)");
                    }
                }
                else
                {
                    subscriptionId = existingSubscription.Id;
                    result.SubscriptionId = existingSubscription.Id;
                    result.StepsCompleted.Add("Subscription already exists");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create subscription for tenant {TenantId}. Continuing with provisioning.", tenantId);
                result.StepsCompleted.Add($"Subscription creation failed: {ex.Message}");
            }

            // Step 3-5: Seed tenant data in a transaction for consistency
            await using (var tx = await tenantCtx.Database.BeginTransactionAsync())
            {
                await SeedTenantModulesAsync(tenantCtx, tenantIdString);
                result.StepsCompleted.Add("Modules seeded");

                await SeedTenantDepartmentsAndPositionsAsync(tenantCtx, tenantIdString);
                result.StepsCompleted.Add("Departments and positions seeded");

                await SeedTenantRolePermissionsAsync(tenantCtx, tenantIdString);
                result.StepsCompleted.Add("Role permissions seeded");

                await tx.CommitAsync();
            }

            // Step 6: Create tenant admin user
            (User? adminUser, string passwordToken) = await CreateTenantAdminUserAsync(
                userManager, 
                adminEmail, 
                adminFirstName, 
                adminLastName,
                tenantId); // Pass tenantId to set on user
            
            if (adminUser == null)
            {
                _logger.LogError("Failed to create admin user for tenant {TenantId}", tenantId);
                return (false, "Failed to create admin user", result);
            }

            result.AdminEmail = adminEmail;
            result.AdminUserId = adminUser.Id;
            result.StepsCompleted.Add("Admin user created");

            // Step 7: Assign Admin role to admin user
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            result.StepsCompleted.Add("Admin role assigned");

            // Step 8: Send invite email with password setup token
            try
            {
                await _emailService.SendTenantAdminInviteEmailAsync(
                    adminEmail,
                    adminFirstName,
                    tenant.Name ?? "Your Organization",
                    passwordToken,
                    adminUser.Id);
                result.EmailSent = true;
                result.StepsCompleted.Add("Welcome email sent");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send invite email for tenant {TenantId}. Provisioning continues.", tenantId);
                result.StepsCompleted.Add($"Email sending failed: {ex.Message}");
            }

            _logger.LogInformation("Successfully provisioned tenant {TenantId}", tenantId);
            return (true, null, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning tenant {TenantId}: {Message}", tenantId, ex.Message);
            return (false, ex.Message, null);
        }
    }

    private async Task EnsureRolesExistAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { "SuperAdmin", "Admin", "HR", "Employee" };
        
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                _logger.LogInformation("Created role {RoleName}", roleName);
            }
        }
    }

    private async Task SeedTenantModulesAsync(ApplicationDbContext context, string tenantId)
    {
        // Seed main modules
        var modules = new[]
        {
            new { Path = "/organization", Name = "Organization", ParentPath = (string?)null, Icon = "ApartmentOutlined", DisplayOrder = 3, Description = "Organization structure management" },
            new { Path = "/departments", Name = "Departments", ParentPath = (string?)"/organization", Icon = "TeamOutlined", DisplayOrder = 1, Description = "Manage departments and organizational units" },
            new { Path = "/positions", Name = "Positions", ParentPath = (string?)"/organization", Icon = "UserOutlined", DisplayOrder = 2, Description = "Manage job positions and titles" },
            new { Path = "/dashboard", Name = "Dashboard", ParentPath = (string?)null, Icon = "DashboardOutlined", DisplayOrder = 1, Description = "Main dashboard" },
            new { Path = "/employees", Name = "Employees", ParentPath = (string?)null, Icon = "UserOutlined", DisplayOrder = 2, Description = "Employee management" }
        };

        foreach (var moduleData in modules)
        {
            var existing = await context.Modules
                .FirstOrDefaultAsync(m => m.Path == moduleData.Path && m.TenantId == tenantId);
            
            if (existing == null)
            {
                var module = new Module
                {
                    TenantId = tenantId,
                    Name = moduleData.Name,
                    Path = moduleData.Path,
                    ParentPath = moduleData.ParentPath,
                    Icon = moduleData.Icon,
                    DisplayOrder = moduleData.DisplayOrder,
                    Description = moduleData.Description,
                    IsActive = true
                };
                await context.Modules.AddAsync(module);
            }
        }
        
        await context.SaveChangesAsync();
    }

    private async Task SeedTenantDepartmentsAndPositionsAsync(ApplicationDbContext context, string tenantId)
    {
        // Seed default departments
        var departmentNames = new[] { "People/HR", "Engineering", "Sales", "Finance", "Customer Support", "Operations" };
        var departments = new Dictionary<string, Department>();
        
        foreach (var deptName in departmentNames)
        {
            var existing = await context.Departments
                .FirstOrDefaultAsync(d => d.Name == deptName && d.TenantId == tenantId);
            
            if (existing == null)
            {
                var department = new Department
                {
                    TenantId = tenantId,
                    Name = deptName,
                    Description = $"{deptName} department",
                    IsActive = true
                };
                await context.Departments.AddAsync(department);
                await context.SaveChangesAsync();
                departments[deptName] = department;
            }
            else
            {
                departments[deptName] = existing;
            }
        }

        // Seed default positions
        var positions = new List<(string Title, string? DepartmentName)>
        {
            ("Employee", null),
            ("Manager", null),
            ("HR Admin", "People/HR"),
            ("Finance Admin", "Finance"),
            ("Support Agent", "Customer Support"),
            ("Engineering — Senior", "Engineering"),
            ("Engineering — Junior", "Engineering"),
            ("Sales Representative", "Sales"),
            ("Sales Manager", "Sales"),
            ("Operations Manager", "Operations"),
            ("HR Manager", "People/HR"),
            ("Finance Manager", "Finance"),
            ("Support Manager", "Customer Support"),
            ("Engineering Manager", "Engineering"),
            ("Senior Engineer", "Engineering"),
            ("Junior Engineer", "Engineering"),
            ("Lead Engineer", "Engineering")
        };

        foreach (var (title, deptName) in positions)
        {
            var existing = await context.Positions
                .FirstOrDefaultAsync(p => p.Title == title && p.TenantId == tenantId);
            
            if (existing == null)
            {
                var position = new Position
                {
                    TenantId = tenantId,
                    Title = title,
                    DepartmentId = deptName != null && departments.ContainsKey(deptName)
                        ? departments[deptName].Id
                        : null,
                    Description = $"Position: {title}",
                    IsActive = true
                };
                await context.Positions.AddAsync(position);
            }
        }
        
        await context.SaveChangesAsync();
    }

    private async Task SeedTenantRolePermissionsAsync(ApplicationDbContext context, string tenantId)
    {
        var roles = new[] { "SuperAdmin", "Admin", "HR", "Employee" };
        var pages = new[]
        {
            new { Path = "/dashboard", Name = "Dashboard", Description = "Main dashboard page" },
            new { Path = "/employees", Name = "Employees", Description = "Employee management" },
            new { Path = "/organization", Name = "Organization", Description = "Organization structure management" },
            new { Path = "/departments", Name = "Departments", Description = "Department management" },
            new { Path = "/positions", Name = "Positions", Description = "Positions management" },
            new { Path = "/calendar", Name = "Calendar", Description = "Calendar and events" },
            new { Path = "/notice-board", Name = "Notice Board", Description = "Company announcements" },
            new { Path = "/expenses", Name = "Expenses", Description = "Expense tracking" },
            new { Path = "/payroll", Name = "Payroll", Description = "Payroll management" },
            new { Path = "/payroll/reports", Name = "Payroll Reports", Description = "Payroll reports" },
            new { Path = "/payroll/settings", Name = "Payroll Settings", Description = "Payroll settings" },
            new { Path = "/settings", Name = "Settings", Description = "User settings" },
            new { Path = "/role-permissions", Name = "Role Permissions", Description = "Manage role permissions" }
        };

        foreach (var role in roles)
        {
            foreach (var page in pages)
            {
                var existing = await context.RolePermissions
                    .FirstOrDefaultAsync(p => p.RoleName == role && p.PagePath == page.Path && p.TenantId == tenantId);
                
                if (existing == null)
                {
                    // Determine page access and action-level permissions
                    bool canAccess = false;
                    bool canView = false;
                    bool canCreate = false;
                    bool canEdit = false;
                    bool canDelete = false;

                    // SuperAdmin: full access everywhere
                    if (role == "SuperAdmin")
                    {
                        canAccess = true;
                        canView = true;
                        canCreate = true;
                        canEdit = true;
                        canDelete = true;
                    }
                    // Admin: access to all except tenant-settings; full actions on allowed pages
                    // Note: Admin has access to /role-permissions for their tenant
                    else if (role == "Admin")
                    {
                        if (page.Path != "/tenant-settings")
                        {
                            canAccess = true;
                            canView = true;
                            canCreate = true;
                            canEdit = true;
                            canDelete = true;
                        }
                    }
                    // HR: access to selected pages; typically view/create/edit, delete restricted
                    else if (role == "HR")
                    {
                        if (page.Path == "/dashboard" || page.Path == "/employees" 
                            || page.Path == "/calendar" || page.Path == "/notice-board" 
                            || page.Path == "/settings" || page.Path == "/payroll"
                            || page.Path == "/payroll/reports" || page.Path == "/payroll/settings")
                        {
                            canAccess = true;
                            canView = true;
                            // HR can create/edit on employees, calendar, notice-board, payroll
                            if (page.Path == "/employees" || page.Path == "/calendar" || page.Path == "/notice-board"
                                || page.Path == "/payroll/reports" || page.Path == "/payroll/settings")
                            {
                                canCreate = true;
                                canEdit = true;
                            }
                            // Deletion typically restricted for HR by default
                            canDelete = false;
                        }
                    }
                    // Employee: limited pages; view only
                    else if (role == "Employee")
                    {
                        if (page.Path == "/dashboard" || page.Path == "/calendar" 
                            || page.Path == "/notice-board" || page.Path == "/settings")
                        {
                            canAccess = true;
                            canView = true;
                        }
                    }

                    var permission = new RolePermission
                    {
                        TenantId = tenantId,
                        RoleName = role,
                        PagePath = page.Path,
                        PageName = page.Name,
                        CanAccess = canAccess,
                        CanView = canView,
                        CanCreate = canCreate,
                        CanEdit = canEdit,
                        CanDelete = canDelete,
                        Description = page.Description
                    };
                    
                    await context.RolePermissions.AddAsync(permission);
                }
            }
        }
        
        await context.SaveChangesAsync();
    }

    private async Task<(User? User, string PasswordToken)> CreateTenantAdminUserAsync(
        UserManager<User> userManager,
        string email,
        string firstName,
        string lastName,
        int tenantId)
    {
        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            _logger.LogInformation("User {Email} already exists, will assign Admin role if not already assigned", email);
            
            // Ensure TenantId is set on existing user if not already set
            if (existingUser.TenantId != tenantId.ToString())
            {
                existingUser.TenantId = tenantId.ToString();
                await userManager.UpdateAsync(existingUser);
                _logger.LogInformation("Updated TenantId for existing user {Email} to {TenantId}", email, tenantId);
            }
            
            // Generate password reset token for existing user to allow password setup
            var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
            return (existingUser, token);
        }

        // Create new user with TenantId set
        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false, // Require email confirmation/password setup
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = DateTime.UtcNow.AddYears(-30), // Default date
            IsActive = true,
            TenantId = tenantId.ToString() // Set TenantId when creating admin user
        };
        
        // Create user with a temporary secure password - they'll reset it via the invite link
        // Since Identity requires a password, we'll generate a random secure password
        // The user will reset it via the token after creation
        var tempPassword = GenerateSecureRandomPassword();
        var result = await userManager.CreateAsync(user, tempPassword);
        
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return (null, string.Empty);
        }

        // Generate password reset token after user is created
        var passwordToken = await userManager.GeneratePasswordResetTokenAsync(user);
        
        _logger.LogInformation("Created admin user {Email} for tenant {TenantId} with TenantId = {TenantId}", 
            email, tenantId, user.TenantId);
        return (user, passwordToken);
    }

    private static string GenerateSecureRandomPassword()
    {
        // Generate a secure random password that meets Identity requirements
        const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        var password = new char[16];
        
        // Ensure at least one of each required character type
        password[0] = char.ToUpper(chars[random.Next(0, 26)]);
        password[1] = chars[random.Next(26, 52)];
        password[2] = chars[random.Next(52, 62)];
        password[3] = chars[random.Next(62, chars.Length)];
        
        // Fill the rest randomly
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }
        
        // Shuffle the password
        return new string(password.OrderBy(x => random.Next()).ToArray());
    }

    // Helper classes for tenant providers
    private class MasterTenantProvider : ITenantProvider
    {
        private readonly string _tenantId;
        
        public MasterTenantProvider(string tenantId)
        {
            _tenantId = tenantId;
        }
        
        public string TenantId => _tenantId;
    }

    private class ProvisioningTenantProvider : ITenantProvider
    {
        private readonly string _tenantId;
        
        public ProvisioningTenantProvider(string tenantId)
        {
            _tenantId = tenantId;
        }
        
        public string TenantId => _tenantId;
    }
}

