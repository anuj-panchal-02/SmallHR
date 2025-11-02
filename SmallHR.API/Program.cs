using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using SmallHR.Infrastructure.Mapping;
using SmallHR.Infrastructure.Repositories;
using SmallHR.Infrastructure.Services;
using SmallHR.API.Services;
using SmallHR.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using SmallHR.API.Authorization;
using System.Text;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "SmallHR API", 
        Version = "v1",
        Description = "A minimal HR Management System API"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database configuration with connection resolver (supports future db-per-tenant)
// Note: ApplicationDbContext uses IServiceProvider to get HttpContextAccessor for SuperAdmin detection
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var resolver = sp.GetRequiredService<IConnectionResolver>();
    var tenantProvider = sp.GetRequiredService<ITenantProvider>();
    if (tenantProvider == null)
    {
        throw new InvalidOperationException("ITenantProvider must be registered and cannot be null");
    }
    var conn = resolver.GetConnectionString(tenantProvider.TenantId);
    options.UseSqlServer(conn);
    // IServiceProvider is injected via constructor to get HttpContextAccessor for SuperAdmin detection
}, ServiceLifetime.Scoped);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();
builder.Services.AddScoped<IConnectionResolver, ConnectionResolver>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITenantCache, TenantMemoryCache>();
builder.Services.AddHostedService<SmallHR.API.HostedServices.ModulesWarmupHostedService>();
builder.Services.AddHostedService<SmallHR.API.HostedServices.TenantProvisioningHostedService>();
builder.Services.AddHostedService<SmallHR.API.HostedServices.TenantLifecycleMonitoringHostedService>();

// Identity configuration
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password requirements
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 3;
    options.User.RequireUniqueEmail = true;
    
    // Lockout settings - prevent brute force attacks
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false; // Will enable after email verification implementation
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"] ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT_SECRET_KEY must be set in environment variables or appsettings.json");
}
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    
    // Extract token from httpOnly cookie
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Check if token is in cookie
            var token = context.Request.Cookies["accessToken"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Repository pattern
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();

// Services
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<IEmailService, ConsoleEmailService>(); // Email service - replace with SMTP/SendGrid in production
builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
builder.Services.AddScoped<ISubscriptionService, SmallHR.Infrastructure.Services.SubscriptionService>();
builder.Services.AddScoped<IUsageMetricsService, SmallHR.Infrastructure.Services.UsageMetricsService>();
builder.Services.AddScoped<ITenantLifecycleService, SmallHR.Infrastructure.Services.TenantLifecycleService>();
builder.Services.AddScoped<IAdminAuditService, SmallHR.Infrastructure.Services.AdminAuditService>();
builder.Services.AddScoped<IAlertService, SmallHR.Infrastructure.Services.AlertService>();

// Webhook handlers
builder.Services.AddScoped<StripeWebhookHandler>();

// Authorization: Permission-based
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Rate limiting configuration
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// CORS - Allow frontend access
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:5173" }; // Default for development
    
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
              .WithHeaders("Content-Type", "Authorization", "X-Tenant-Id", "X-Tenant-Domain")
              .AllowCredentials();
    });
});

// Logging
builder.Services.AddLogging();

// Request size limits
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760; // 10MB
    options.ValueLengthLimit = 10485760;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmallHR API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

// Security headers middleware (must be early in pipeline)
app.UseMiddleware<SecurityHeadersMiddleware>();

// Rate limiting must be before authentication
app.UseIpRateLimiting();

// CORS must come before Authentication and Authorization
app.UseCors("AllowFrontend");

// Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Only redirect to HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();

// SuperAdmin query filter bypass (must be before feature access middleware)
app.UseMiddleware<SuperAdminQueryFilterBypassMiddleware>();

// Feature access middleware (checks subscription status and active features)
app.UseMiddleware<FeatureAccessMiddleware>();

app.UseAuthorization();

// Admin audit middleware (must be after authorization to access user claims)
app.UseMiddleware<AdminAuditMiddleware>();

app.MapControllers();

// Development endpoints
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/dev/check-users", async (UserManager<User> userManager) =>
    {
        var users = userManager.Users.ToList();
        var userInfo = new List<object>();
        
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userInfo.Add(new
            {
                Email = user.Email,
                Name = $"{user.FirstName} {user.LastName}",
                Roles = roles,
                IsActive = user.IsActive
            });
        }
        
        return Results.Ok(new { count = users.Count, users = userInfo });
    });
    
    // Clean up demo/test data (keeps roles and SuperAdmin)
    app.MapPost("/api/dev/cleanup-demo-data", async (ApplicationDbContext context, UserManager<User> userManager) =>
    {
        try
        {
            int deletedEmployees = 0;
            int deletedLeaveRequests = 0;
            int deletedAttendances = 0;
            int deletedDemoUsers = 0;

            // Delete demo users (excluding SuperAdmin)
            var demoUserEmails = new[] { "admin@smallhr.com", "hr@smallhr.com", "employee@smallhr.com" };
            var demoUsers = userManager.Users.Where(u => demoUserEmails.Contains(u.Email)).ToList();
            foreach (var user in demoUsers)
            {
                await userManager.DeleteAsync(user);
                deletedDemoUsers++;
            }

            // Delete all employees
            var employees = await context.Employees.ToListAsync();
            if (employees.Any())
            {
                deletedEmployees = employees.Count;
                context.Employees.RemoveRange(employees);
                await context.SaveChangesAsync();
            }

            // Delete all leave requests
            var leaveRequests = await context.LeaveRequests.ToListAsync();
            if (leaveRequests.Any())
            {
                deletedLeaveRequests = leaveRequests.Count;
                context.LeaveRequests.RemoveRange(leaveRequests);
                await context.SaveChangesAsync();
            }

            // Delete all attendance records
            var attendances = await context.Attendances.ToListAsync();
            if (attendances.Any())
            {
                deletedAttendances = attendances.Count;
                context.Attendances.RemoveRange(attendances);
                await context.SaveChangesAsync();
            }

            return Results.Ok(new 
            { 
                message = "Demo data cleaned successfully",
                deleted = new
                {
                    Employees = deletedEmployees,
                    LeaveRequests = deletedLeaveRequests,
                    Attendances = deletedAttendances,
                    DemoUsers = deletedDemoUsers
                },
                note = "Roles and SuperAdmin user are preserved"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error cleaning demo data: {ex.Message}");
        }
    });
    
    // Delete all users except SuperAdmin
    app.MapPost("/api/dev/cleanup-users", async (UserManager<User> userManager) =>
    {
        try
        {
            const string superAdminEmail = "superadmin@smallhr.com";
            int deletedCount = 0;
            var errors = new List<string>();

            // Get all users except SuperAdmin
            var usersToDelete = userManager.Users
                .Where(u => u.Email != null && !u.Email.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!usersToDelete.Any())
            {
                return Results.Ok(new 
                { 
                    message = "No users to delete. Only SuperAdmin exists.",
                    deletedCount = 0,
                    superAdminEmail = superAdminEmail
                });
            }

            // Delete each user
            foreach (var user in usersToDelete)
            {
                try
                {
                    var result = await userManager.DeleteAsync(user);
                    if (result.Succeeded)
                    {
                        deletedCount++;
                    }
                    else
                    {
                        errors.Add($"{user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{user.Email}: {ex.Message}");
                }
            }

            return Results.Ok(new 
            { 
                message = $"Cleanup completed. {deletedCount} user(s) deleted.",
                deletedCount = deletedCount,
                totalUsers = usersToDelete.Count,
                superAdminEmail = superAdminEmail,
                errors = errors.Any() ? errors : null
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error cleaning up users: {ex.Message}");
        }
    });
    
    // Full database reset (drops and recreates database, keeps only 1 SuperAdmin)
    app.MapPost("/api/dev/reset-database", async (ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ILogger<Program> logger) =>
    {
        try
        {
            // Delete all data and recreate database
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            
            // Apply all migrations
            await context.Database.MigrateAsync();
            
            // Re-seed with clean data (roles + 1 SuperAdmin only)
            await SeedDataAsync(context, userManager, roleManager, logger);
            
            return Results.Ok(new { 
                message = "Database reset and re-seeded successfully. Only 1 SuperAdmin user exists.",
                superAdminEmail = "superadmin@smallhr.com",
                superAdminPassword = "SuperAdmin@123"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error resetting database: {ex.Message}");
        }
    });
    
    // Clean all data but keep roles and 1 SuperAdmin (without dropping database)
    app.MapPost("/api/dev/clean-all-data", async (ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ILogger<Program> logger) =>
    {
        try
        {
            // Delete all tenant-related data in reverse order of dependencies
            logger.LogInformation("Starting cleanup of all tenant data...");
            
            // Helper function to safely delete from DbSet if table exists
            // Uses raw SQL to bypass all EF Core query filters and ensure ALL records are deleted
            async Task<int> SafeDeleteAsync<T>(DbSet<T> dbSet, string tableName) where T : class
            {
                try
                {
                    // First, get the actual table name from EF Core metadata
                    var entityType = context.Model.FindEntityType(typeof(T));
                    if (entityType == null)
                    {
                        logger.LogWarning("Entity type not found for {TableName}. Skipping.", tableName);
                        return 0;
                    }
                    
                    var sqlTableName = entityType.GetTableName() ?? entityType.GetDefaultTableName() ?? tableName;
                    
                    // Get count using raw SQL (bypasses all query filters)
                    var connection = context.Database.GetDbConnection();
                    await connection.OpenAsync();
                    int count = 0;
                    try
                    {
                        using var countCommand = connection.CreateCommand();
                        countCommand.CommandText = $"SELECT COUNT(*) FROM [{sqlTableName}]";
                        var countResult = await countCommand.ExecuteScalarAsync();
                        count = countResult != null ? Convert.ToInt32(countResult) : 0;
                    }
                    finally
                    {
                        await connection.CloseAsync();
                    }
                    
                    if (count > 0)
                    {
                        // Use raw SQL DELETE to ensure ALL records are removed (bypasses query filters)
                        await context.Database.ExecuteSqlRawAsync($"DELETE FROM [{sqlTableName}]");
                        logger.LogInformation("Deleted {Count} records from {TableName} using raw SQL", count, sqlTableName);
                    }
                    return count;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Table {TableName} may not exist yet or is empty. Skipping.", tableName);
                    return 0;
                }
            }
            
            // Delete tenant-related entities first (child tables)
            var alertsDeleted = await SafeDeleteAsync(context.Alerts, "Alerts");
            var webhookEventsDeleted = await SafeDeleteAsync(context.WebhookEvents, "WebhookEvents");
            var adminAuditsDeleted = await SafeDeleteAsync(context.AdminAudits, "AdminAudits");
            
            var lifecycleEventsDeleted = await SafeDeleteAsync(context.TenantLifecycleEvents, "TenantLifecycleEvents");
            var usageMetricsDeleted = await SafeDeleteAsync(context.TenantUsageMetrics, "TenantUsageMetrics");
            var subscriptionPlanFeaturesDeleted = await SafeDeleteAsync(context.SubscriptionPlanFeatures, "SubscriptionPlanFeatures");
            var subscriptionsDeleted = await SafeDeleteAsync(context.Subscriptions, "Subscriptions");
            var rolePermissionsDeleted = await SafeDeleteAsync(context.RolePermissions, "RolePermissions");
            var modulesDeleted = await SafeDeleteAsync(context.Modules, "Modules");
            var positionsDeleted = await SafeDeleteAsync(context.Positions, "Positions");
            var departmentsDeleted = await SafeDeleteAsync(context.Departments, "Departments");
            var attendancesDeleted = await SafeDeleteAsync(context.Attendances, "Attendances");
            var leaveRequestsDeleted = await SafeDeleteAsync(context.LeaveRequests, "LeaveRequests");
            var employeesDeleted = await SafeDeleteAsync(context.Employees, "Employees");
            
            // Delete tenants last (parent table)
            var tenantsDeleted = await SafeDeleteAsync(context.Tenants, "Tenants");
            
            await context.SaveChangesAsync();
            
            logger.LogInformation("Deleted tenant data: Tenants={Tenants}, Employees={Employees}, LeaveRequests={LeaveRequests}, Attendances={Attendances}, Alerts={Alerts}, WebhookEvents={WebhookEvents}",
                tenantsDeleted, employeesDeleted, leaveRequestsDeleted, attendancesDeleted, alertsDeleted, webhookEventsDeleted);
            
            // Delete all users except SuperAdmin
            var superAdminEmail = "superadmin@smallhr.com";
            var allUsers = userManager.Users.ToList();
            foreach (var user in allUsers)
            {
                if (user.Email == null || !user.Email.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    await userManager.DeleteAsync(user);
                }
            }
            
            // Ensure only 1 SuperAdmin exists
            await SeedDataAsync(context, userManager, roleManager, logger);
            
            return Results.Ok(new { 
                message = "All data cleaned successfully. Only 1 SuperAdmin user and roles remain.",
                superAdminEmail = "superadmin@smallhr.com",
                superAdminPassword = "SuperAdmin@123"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error cleaning data: {ex.Message}");
        }
    });
}

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        await SeedDataAsync(context, userManager, roleManager, logger);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

// Seed data method - Minimal setup: Only roles and 1 SuperAdmin
static async Task SeedDataAsync(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ILogger logger)
{
    // Create roles only - essential for the system
    if (!await roleManager.RoleExistsAsync("SuperAdmin"))
    {
        await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
    }
    
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    
    if (!await roleManager.RoleExistsAsync("HR"))
    {
        await roleManager.CreateAsync(new IdentityRole("HR"));
    }
    
    if (!await roleManager.RoleExistsAsync("Employee"))
    {
        await roleManager.CreateAsync(new IdentityRole("Employee"));
    }

    // Delete all existing users except SuperAdmin (to ensure only 1 SuperAdmin exists)
    var superAdminEmail = "superadmin@smallhr.com";
    var allUsers = userManager.Users.ToList();
    foreach (var user in allUsers)
    {
        if (user.Email == null || !user.Email.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            await userManager.DeleteAsync(user);
            logger.LogInformation("Deleted user: {Email}", user.Email);
        }
    }

    // Delete duplicate SuperAdmin users (keep only one)
    var allSuperAdmins = await userManager.GetUsersInRoleAsync("SuperAdmin");
    if (allSuperAdmins.Count > 1)
    {
        // Keep the first one, delete the rest
        var superAdminToKeep = allSuperAdmins.FirstOrDefault(u => u.Email != null && u.Email.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase))
            ?? allSuperAdmins.First();
        
        foreach (var admin in allSuperAdmins)
        {
            if (admin.Id != superAdminToKeep.Id)
            {
                await userManager.DeleteAsync(admin);
                logger.LogInformation("Deleted duplicate SuperAdmin: {Email}", admin.Email);
            }
        }
    }

    // Create or update SuperAdmin user (ensure only 1 exists)
    var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);
    if (superAdminUser == null)
    {
        superAdminUser = new User
        {
            UserName = superAdminEmail,
            Email = superAdminEmail,
            FirstName = "Super",
            LastName = "Admin",
            DateOfBirth = new DateTime(1985, 1, 1),
            IsActive = true,
            TenantId = null // SuperAdmin operates at platform layer, no tenant association
        };
        
        await userManager.CreateAsync(superAdminUser, "SuperAdmin@123");
        await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
        logger.LogInformation("Created SuperAdmin user: {Email}", superAdminEmail);
    }
    else
    {
        // Ensure existing SuperAdmin has TenantId = null and correct role
        if (superAdminUser.TenantId != null)
        {
            superAdminUser.TenantId = null;
            await userManager.UpdateAsync(superAdminUser);
            logger.LogInformation("Updated existing SuperAdmin user to have TenantId = null");
        }
        
        // Ensure SuperAdmin role is assigned
        if (!await userManager.IsInRoleAsync(superAdminUser, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
            logger.LogInformation("Assigned SuperAdmin role to existing user: {Email}", superAdminEmail);
        }
    }
}

// Seed demo users for all roles (Development only)
static async Task SeedDemoUsersForAllRolesAsync(UserManager<User> userManager)
{
    var demoUsers = new[]
    {
        new { Email = "admin@smallhr.com", Password = "Admin@123", Role = "Admin", FirstName = "Admin", LastName = "User", DateOfBirth = new DateTime(1990, 5, 15) },
        new { Email = "hr@smallhr.com", Password = "Hr@123", Role = "HR", FirstName = "HR", LastName = "Manager", DateOfBirth = new DateTime(1988, 8, 20) },
        new { Email = "employee@smallhr.com", Password = "Employee@123", Role = "Employee", FirstName = "John", LastName = "Employee", DateOfBirth = new DateTime(1995, 3, 10) }
    };

    foreach (var demoUser in demoUsers)
    {
        var existingUser = await userManager.FindByEmailAsync(demoUser.Email);
        if (existingUser == null)
        {
            var user = new User
            {
                UserName = demoUser.Email,
                Email = demoUser.Email,
                FirstName = demoUser.FirstName,
                LastName = demoUser.LastName,
                DateOfBirth = demoUser.DateOfBirth,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, demoUser.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, demoUser.Role);
            }
        }
    }
}
