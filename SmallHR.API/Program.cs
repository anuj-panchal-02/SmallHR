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
}, ServiceLifetime.Scoped);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();
builder.Services.AddScoped<IConnectionResolver, ConnectionResolver>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITenantCache, TenantMemoryCache>();
builder.Services.AddHostedService<SmallHR.API.HostedServices.ModulesWarmupHostedService>();

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
app.UseAuthorization();

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
    
    // Full database reset (drops and recreates database)
    app.MapPost("/api/dev/reset-database", async (ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager) =>
    {
        try
        {
            // Delete all data and recreate database
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            
            // Re-seed with clean data (roles + SuperAdmin only)
            await SeedDataAsync(context, userManager, roleManager);
            
            return Results.Ok(new { message = "Database reset and re-seeded successfully (fresh SaaS setup)" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error resetting database: {ex.Message}");
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
        
        await SeedDataAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

// Seed data method - Fresh SaaS setup
static async Task SeedDataAsync(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
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

    // Create SuperAdmin user for initial setup (always created)
    var superAdminEmail = "superadmin@smallhr.com";
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
            IsActive = true
        };
        
        await userManager.CreateAsync(superAdminUser, "SuperAdmin@123");
        await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
    }

    // Development only: Create demo users for all roles
    // This helps with testing and development without manual user creation
    // These users are only created in Development environment
    // Production deployments should create users through the UI/API after setup
    var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    if (isDevelopment)
    {
        await SeedDemoUsersForAllRolesAsync(userManager);
    }

    // Seed departments and positions (essential for employee management)
    const string defaultTenantId = "default";
    
    // Seed Modules - Organization master module with Departments and Positions as children
    var organizationModule = await context.Modules
        .FirstOrDefaultAsync(m => m.Path == "/organization" && m.TenantId == defaultTenantId);
    
    if (organizationModule == null)
    {
        organizationModule = new Module
        {
            TenantId = defaultTenantId,
            Name = "Organization",
            Path = "/organization",
            ParentPath = null,
            Icon = "ApartmentOutlined",
            DisplayOrder = 3,
            Description = "Organization structure management",
            IsActive = true
        };
        await context.Modules.AddAsync(organizationModule);
        await context.SaveChangesAsync();
    }
    else
    {
        // Update DisplayOrder if it was changed
        if (organizationModule.DisplayOrder != 3)
        {
            organizationModule.DisplayOrder = 3;
            await context.SaveChangesAsync();
        }
    }
    
    // Departments module
    var departmentsModule = await context.Modules
        .FirstOrDefaultAsync(m => m.Path == "/departments" && m.TenantId == defaultTenantId);
    
    if (departmentsModule == null)
    {
        departmentsModule = new Module
        {
            TenantId = defaultTenantId,
            Name = "Departments",
            Path = "/departments",
            ParentPath = "/organization",
            Icon = "TeamOutlined",
            DisplayOrder = 1,
            Description = "Manage departments and organizational units",
            IsActive = true
        };
        await context.Modules.AddAsync(departmentsModule);
        await context.SaveChangesAsync();
    }
    
    // Positions module
    var positionsModule = await context.Modules
        .FirstOrDefaultAsync(m => m.Path == "/positions" && m.TenantId == defaultTenantId);
    
    if (positionsModule == null)
    {
        positionsModule = new Module
        {
            TenantId = defaultTenantId,
            Name = "Positions",
            Path = "/positions",
            ParentPath = "/organization",
            Icon = "UserOutlined",
            DisplayOrder = 2,
            Description = "Manage job positions and titles",
            IsActive = true
        };
        await context.Modules.AddAsync(positionsModule);
        await context.SaveChangesAsync();
    }
    
    // Seed Departments
    var departmentNames = new[] { "People/HR", "Engineering", "Sales", "Finance", "Customer Support", "Operations" };
    var departments = new Dictionary<string, Department>();
    
    foreach (var deptName in departmentNames)
    {
        var existingDept = await context.Departments
            .FirstOrDefaultAsync(d => d.Name == deptName && d.TenantId == defaultTenantId);
        
        if (existingDept == null)
        {
            var department = new Department
            {
                TenantId = defaultTenantId,
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
            departments[deptName] = existingDept;
        }
    }
    
    // Seed Positions
    var positions = new List<(string Title, string? DepartmentName)>
    {
        ("Employee", null), // Base position - can be in any department
        ("Manager", null), // Manager position - can be in any department
        ("HR Admin", "People/HR"),
        ("Finance Admin", "Finance"),
        ("Support Agent", "Customer Support"),
        ("Engineering — Senior", "Engineering"),
        ("Engineering — Junior", "Engineering"),
        // Additional important positions
        ("Sales Representative", "Sales"),
        ("Sales Manager", "Sales"),
        ("Operations Manager", "Operations"),
        ("Operations Coordinator", "Operations"),
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
        var existingPos = await context.Positions
            .FirstOrDefaultAsync(p => p.Title == title && p.TenantId == defaultTenantId);
        
        if (existingPos == null)
        {
            var position = new Position
            {
                TenantId = defaultTenantId,
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
    
    // Seed Role Permissions for new pages (departments, positions, organization)
    var newPages = new[]
    {
        new { Path = "/departments", Name = "Departments", Description = "Manage departments and organizational units" },
        new { Path = "/positions", Name = "Positions", Description = "Manage job positions and titles" },
        new { Path = "/organization", Name = "Organization", Description = "Organization structure management" }
    };
    
    var roles = new[] { "SuperAdmin", "Admin", "HR", "Employee" };
    
    foreach (var role in roles)
    {
        foreach (var page in newPages)
        {
            // Check if permission already exists
            var existingPermission = await context.RolePermissions
                .FirstOrDefaultAsync(p => p.RoleName == role && p.PagePath == page.Path && p.TenantId == defaultTenantId);
            
            if (existingPermission == null)
            {
                // Determine permissions based on role
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
                // Admin: full access to organization management
                else if (role == "Admin")
                {
                    canAccess = true;
                    canView = true;
                    canCreate = true;
                    canEdit = true;
                    canDelete = true;
                }
                // HR: view and edit access to organization structure
                else if (role == "HR")
                {
                    canAccess = true;
                    canView = true;
                    canCreate = true;
                    canEdit = true;
                    canDelete = false; // HR cannot delete departments/positions
                }
                // Employee: view only
                else if (role == "Employee")
                {
                    canAccess = true;
                    canView = true;
                    canCreate = false;
                    canEdit = false;
                    canDelete = false;
                }
                
                var permission = new RolePermission
                {
                    TenantId = defaultTenantId,
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
