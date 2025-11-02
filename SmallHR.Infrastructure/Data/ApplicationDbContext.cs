using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using System.Security.Claims;

namespace SmallHR.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IServiceProvider? _serviceProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
    }

    // Constructor with IServiceProvider for SuperAdmin detection via HttpContextAccessor
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options, 
        ITenantProvider tenantProvider,
        IServiceProvider serviceProvider) : base(options)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets HttpContextAccessor from service provider if available
    /// </summary>
    private IHttpContextAccessor? GetHttpContextAccessor()
    {
        return _serviceProvider?.GetService<IHttpContextAccessor>();
    }

    /// <summary>
    /// Checks if the current user is a SuperAdmin
    /// </summary>
    private bool IsSuperAdmin()
    {
        var httpContextAccessor = GetHttpContextAccessor();
        return httpContextAccessor?.HttpContext?.User?.IsInRole("SuperAdmin") == true;
    }

    /// <summary>
    /// Checks if the current user should bypass tenant isolation
    /// </summary>
    private bool ShouldBypassTenantIsolation()
    {
        return IsSuperAdmin();
    }

    /// <summary>
    /// Checks if tenant query filters should be bypassed
    /// Only bypassed for SuperAdmin on specific admin endpoints
    /// </summary>
    private bool ShouldBypassTenantQueryFilters()
    {
        if (!IsSuperAdmin())
            return false;

        var httpContextAccessor = GetHttpContextAccessor();
        var httpContext = httpContextAccessor?.HttpContext;
        if (httpContext == null)
            return false;

        // Check if bypass is explicitly enabled for this request
        return httpContext.Items.ContainsKey("BypassTenantQueryFilters") &&
               httpContext.Items["BypassTenantQueryFilters"] is bool bypass &&
               bypass;
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Position> Positions { get; set; }
    
    // Subscription entities
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Feature> Features { get; set; }
    public DbSet<SubscriptionPlanFeature> SubscriptionPlanFeatures { get; set; }
    
    // Usage metrics
    public DbSet<TenantUsageMetrics> TenantUsageMetrics { get; set; }
    
    // Lifecycle management
    public DbSet<TenantLifecycleEvent> TenantLifecycleEvents { get; set; }
    
    // Admin audit
    public DbSet<AdminAudit> AdminAudits { get; set; }
    
    // Webhook events
    public DbSet<WebhookEvent> WebhookEvents { get; set; }
    
    // Alerts
    public DbSet<Alert> Alerts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Employee configuration
        builder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.EmployeeId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Position).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Department).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Salary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.ZipCode).HasMaxLength(20);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(100);
            entity.Property(e => e.EmergencyContactPhone).HasMaxLength(20);
            entity.Property(e => e.EmergencyContactRelationship).HasMaxLength(50);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.EmployeeId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.TenantId);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Employees)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            // Query filter for tenant isolation
            // Bypassed temporarily for SuperAdmin on admin endpoints via HttpContext.Items["BypassTenantQueryFilters"]
            entity.HasQueryFilter(e => ShouldBypassTenantQueryFilters() || e.TenantId == _tenantProvider.TenantId);
        });

        // LeaveRequest configuration
        builder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(lr => lr.Id);
            entity.Property(lr => lr.TenantId).IsRequired().HasMaxLength(64);
            entity.Property(lr => lr.LeaveType).IsRequired().HasMaxLength(50);
            entity.Property(lr => lr.Reason).IsRequired().HasMaxLength(500);
            entity.Property(lr => lr.Comments).HasMaxLength(1000);
            entity.Property(lr => lr.Status).IsRequired().HasMaxLength(20);
            entity.Property(lr => lr.ApprovedBy).HasMaxLength(256);
            entity.Property(lr => lr.RejectionReason).HasMaxLength(500);

            entity.HasOne(lr => lr.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(lr => lr.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(lr => lr.TenantId);
            // Query filter for tenant isolation
            entity.HasQueryFilter(lr => ShouldBypassTenantQueryFilters() || lr.TenantId == _tenantProvider.TenantId);
        });

        // Attendance configuration
        builder.Entity<Attendance>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.TenantId).IsRequired().HasMaxLength(64);
            entity.Property(a => a.Status).IsRequired().HasMaxLength(20);
            entity.Property(a => a.Notes).HasMaxLength(500);

            entity.HasOne(a => a.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
            entity.HasIndex(a => a.TenantId);
            // Query filter for tenant isolation
            entity.HasQueryFilter(a => ShouldBypassTenantQueryFilters() || a.TenantId == _tenantProvider.TenantId);
        });

        // User configuration
        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.TenantId).HasMaxLength(64);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Address).HasMaxLength(500);
            entity.Property(u => u.City).HasMaxLength(100);
            entity.Property(u => u.State).HasMaxLength(100);
            entity.Property(u => u.ZipCode).HasMaxLength(20);
            entity.Property(u => u.Country).HasMaxLength(100);
            
            entity.HasIndex(u => u.TenantId);
        });

        // RolePermission configuration
        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => rp.Id);
            entity.Property(rp => rp.TenantId).IsRequired().HasMaxLength(64);
            entity.Property(rp => rp.RoleName).IsRequired().HasMaxLength(50);
            entity.Property(rp => rp.PagePath).IsRequired().HasMaxLength(200);
            entity.Property(rp => rp.PageName).IsRequired().HasMaxLength(100);
            entity.Property(rp => rp.Description).HasMaxLength(500);

            entity.HasIndex(rp => new { rp.TenantId, rp.RoleName, rp.PagePath }).IsUnique();
            // Query filter for tenant isolation
            entity.HasQueryFilter(rp => ShouldBypassTenantQueryFilters() || rp.TenantId == _tenantProvider.TenantId);
        });

        // Module configuration
        builder.Entity<Module>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.TenantId).IsRequired().HasMaxLength(64);
            entity.Property(m => m.Name).IsRequired().HasMaxLength(100);
            entity.Property(m => m.Path).IsRequired().HasMaxLength(200);
            entity.Property(m => m.ParentPath).HasMaxLength(200);
            entity.Property(m => m.Icon).HasMaxLength(100);
            entity.Property(m => m.Description).HasMaxLength(500);
            entity.HasIndex(m => new { m.TenantId, m.Path }).IsUnique();
            entity.HasIndex(m => new { m.TenantId, m.ParentPath, m.DisplayOrder });
            // Query filter for tenant isolation
            entity.HasQueryFilter(m => ShouldBypassTenantQueryFilters() || m.TenantId == _tenantProvider.TenantId);
        });

        // Tenant configuration
        builder.Entity<Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Domain).HasMaxLength(200);
            entity.Property(t => t.SubscriptionPlan).IsRequired().HasMaxLength(50);
            entity.Property(t => t.MaxEmployees);
            entity.Property(t => t.IsSubscriptionActive);
            entity.Property(t => t.SubscriptionStartDate);
            entity.Property(t => t.SubscriptionEndDate);
            entity.HasIndex(t => t.Domain);
        });

        // Department configuration
        builder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.TenantId).IsRequired().HasMaxLength(64);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Description).HasMaxLength(500);
            
            entity.HasOne(d => d.HeadOfDepartment)
                .WithMany()
                .HasForeignKey(d => d.HeadOfDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(d => new { d.TenantId, d.Name }).IsUnique();
            entity.HasIndex(d => d.TenantId);
            entity.HasIndex(d => d.HeadOfDepartmentId);
            // Query filter for tenant isolation
            entity.HasQueryFilter(d => ShouldBypassTenantQueryFilters() || d.TenantId == _tenantProvider.TenantId);
        });

        // Position configuration
        builder.Entity<Position>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.TenantId).IsRequired().HasMaxLength(64);
            entity.Property(p => p.Title).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Description).HasMaxLength(500);
            
            entity.HasOne(p => p.Department)
                .WithMany(d => d.Positions)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(p => new { p.TenantId, p.Title }).IsUnique();
            entity.HasIndex(p => p.TenantId);
            entity.HasIndex(p => p.DepartmentId);
            // Query filter for tenant isolation
            entity.HasQueryFilter(p => ShouldBypassTenantQueryFilters() || p.TenantId == _tenantProvider.TenantId);
        });

        // Subscription configuration
        builder.Entity<Subscription>(entity =>
        {
            entity.HasKey(s => s.Id);
            
            entity.HasOne(s => s.Tenant)
                .WithMany()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(s => s.Plan)
                .WithMany()
                .HasForeignKey(s => s.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.Property(s => s.Currency).HasMaxLength(10).HasDefaultValue("USD");
            entity.Property(s => s.Price).HasColumnType("decimal(18,2)");
            entity.Property(s => s.ExternalSubscriptionId).HasMaxLength(255);
            entity.Property(s => s.ExternalCustomerId).HasMaxLength(255);
            entity.Property(s => s.CancellationReason).HasMaxLength(500);
            entity.Property(s => s.Notes).HasMaxLength(1000);
            
            entity.HasIndex(s => s.TenantId).IsUnique(); // One active subscription per tenant
            entity.HasIndex(s => s.ExternalSubscriptionId);
            entity.HasIndex(s => s.ExternalCustomerId);
            entity.HasIndex(s => s.Status);
        });

        // SubscriptionPlan configuration
        builder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(sp => sp.Id);
            
            entity.Property(sp => sp.Name).IsRequired().HasMaxLength(100);
            entity.Property(sp => sp.Description).HasMaxLength(500);
            entity.Property(sp => sp.Currency).HasMaxLength(10).HasDefaultValue("USD");
            entity.Property(sp => sp.MonthlyPrice).HasColumnType("decimal(18,2)");
            entity.Property(sp => sp.YearlyPrice).HasColumnType("decimal(18,2)");
            entity.Property(sp => sp.QuarterlyPrice).HasColumnType("decimal(18,2)");
            entity.Property(sp => sp.StripePriceId).HasMaxLength(255);
            entity.Property(sp => sp.StripeYearlyPriceId).HasMaxLength(255);
            entity.Property(sp => sp.PaddlePlanId).HasMaxLength(255);
            entity.Property(sp => sp.PopularBadge).HasMaxLength(50);
            entity.Property(sp => sp.Icon).HasMaxLength(255);
            
            entity.HasIndex(sp => sp.Name).IsUnique();
            entity.HasIndex(sp => sp.IsActive);
            entity.HasIndex(sp => sp.IsVisible);
        });

        // Feature configuration
        builder.Entity<Feature>(entity =>
        {
            entity.HasKey(f => f.Id);
            
            entity.Property(f => f.Key).IsRequired().HasMaxLength(100);
            entity.Property(f => f.Name).IsRequired().HasMaxLength(200);
            entity.Property(f => f.Description).HasMaxLength(500);
            entity.Property(f => f.Category).HasMaxLength(100);
            entity.Property(f => f.DefaultValue).HasMaxLength(255);
            
            entity.HasIndex(f => f.Key).IsUnique();
            entity.HasIndex(f => f.Category);
        });

        // SubscriptionPlanFeature configuration (Many-to-Many)
        builder.Entity<SubscriptionPlanFeature>(entity =>
        {
            entity.HasKey(spf => spf.Id);
            
            entity.HasOne(spf => spf.Plan)
                .WithMany(sp => sp.PlanFeatures)
                .HasForeignKey(spf => spf.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(spf => spf.Feature)
                .WithMany(f => f.PlanFeatures)
                .HasForeignKey(spf => spf.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(spf => spf.Value).HasMaxLength(255);
            
            entity.HasIndex(spf => new { spf.SubscriptionPlanId, spf.FeatureId }).IsUnique();
        });

        // TenantUsageMetrics configuration
        builder.Entity<TenantUsageMetrics>(entity =>
        {
            entity.HasKey(m => m.Id);
            
            entity.HasOne(m => m.Tenant)
                .WithMany()
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(m => m.StorageBytesUsed).HasColumnType("bigint");
            entity.Property(m => m.DataTransferBytes).HasColumnType("bigint");
            entity.Property(m => m.ApiRequestCount).HasColumnType("bigint");
            entity.Property(m => m.ApiRequestCountToday).HasColumnType("bigint");
            entity.Property(m => m.FeatureUsageJson).HasMaxLength(4000); // JSON serialized
            
            // Ignore FeatureUsage dictionary - we use FeatureUsageJson for storage
            entity.Ignore(m => m.FeatureUsage);
            
            entity.HasIndex(m => new { m.TenantId, m.PeriodStart, m.PeriodEnd });
            entity.HasIndex(m => m.TenantId);
            entity.HasIndex(m => m.PeriodStart);
        });

        // TenantLifecycleEvent configuration
        builder.Entity<TenantLifecycleEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.LifecycleEvents)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.MetadataJson).HasMaxLength(4000); // JSON serialized
            
            // Ignore Metadata dictionary - we use MetadataJson for storage
            entity.Ignore(e => e.Metadata);
            
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.EventDate);
            entity.HasIndex(e => new { e.TenantId, e.EventDate });
        });

        // AdminAudit configuration
        builder.Entity<AdminAudit>(entity =>
        {
            entity.HasKey(a => a.Id);
            
            entity.Property(a => a.AdminUserId).IsRequired().HasMaxLength(450);
            entity.Property(a => a.AdminEmail).IsRequired().HasMaxLength(256);
            entity.Property(a => a.ActionType).IsRequired().HasMaxLength(100);
            entity.Property(a => a.HttpMethod).IsRequired().HasMaxLength(10);
            entity.Property(a => a.Endpoint).IsRequired().HasMaxLength(500);
            entity.Property(a => a.TargetTenantId).HasMaxLength(64);
            entity.Property(a => a.TargetEntityType).HasMaxLength(100);
            entity.Property(a => a.TargetEntityId).HasMaxLength(450);
            entity.Property(a => a.RequestPayload).HasMaxLength(4000); // JSON
            entity.Property(a => a.IpAddress).HasMaxLength(45);
            entity.Property(a => a.UserAgent).HasMaxLength(500);
            entity.Property(a => a.Metadata).HasMaxLength(4000); // JSON
            entity.Property(a => a.ErrorMessage).HasMaxLength(2000);
            
            // No query filter - AdminAudit is platform-level, not tenant-scoped
            entity.HasIndex(a => a.AdminUserId);
            entity.HasIndex(a => a.AdminEmail);
            entity.HasIndex(a => a.ActionType);
            entity.HasIndex(a => a.TargetTenantId);
            entity.HasIndex(a => a.CreatedAt);
            entity.HasIndex(a => new { a.AdminUserId, a.CreatedAt });
            entity.HasIndex(a => new { a.ActionType, a.CreatedAt });
        });

        // WebhookEvent configuration
        builder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(w => w.Id);
            
            entity.Property(w => w.EventType).IsRequired().HasMaxLength(100);
            entity.Property(w => w.Provider).IsRequired().HasMaxLength(50);
            entity.Property(w => w.Payload).IsRequired(); // JSON payload
            entity.Property(w => w.Error).HasMaxLength(2000);
            entity.Property(w => w.Signature).HasMaxLength(500);
            
            entity.HasOne(w => w.Tenant)
                .WithMany()
                .HasForeignKey(w => w.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(w => w.Subscription)
                .WithMany()
                .HasForeignKey(w => w.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(w => w.EventType);
            entity.HasIndex(w => w.Provider);
            entity.HasIndex(w => w.Processed);
            entity.HasIndex(w => w.TenantId);
            entity.HasIndex(w => w.SubscriptionId);
            entity.HasIndex(w => w.CreatedAt);
            entity.HasIndex(w => new { w.Provider, w.Processed, w.CreatedAt });
        });

        // Alert configuration
        builder.Entity<Alert>(entity =>
        {
            entity.HasKey(a => a.Id);
            
            entity.Property(a => a.TenantId).IsRequired();
            entity.Property(a => a.AlertType).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Severity).IsRequired().HasMaxLength(20);
            entity.Property(a => a.Message).IsRequired().HasMaxLength(500);
            entity.Property(a => a.Status).IsRequired().HasMaxLength(20);
            entity.Property(a => a.ResolvedBy).HasMaxLength(450);
            entity.Property(a => a.ResolutionNotes).HasMaxLength(2000);
            entity.Property(a => a.MetadataJson).HasMaxLength(4000); // JSON serialized
            
            // Ignore Metadata dictionary - we use MetadataJson for storage
            entity.Ignore(a => a.Metadata);
            
            entity.HasOne(a => a.Tenant)
                .WithMany()
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(a => a.Subscription)
                .WithMany()
                .HasForeignKey(a => a.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(a => a.TenantId);
            entity.HasIndex(a => a.AlertType);
            entity.HasIndex(a => a.Severity);
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.CreatedAt);
            entity.HasIndex(a => new { a.TenantId, a.Status, a.CreatedAt });
            entity.HasIndex(a => new { a.AlertType, a.Status });
            entity.HasIndex(a => new { a.Severity, a.Status });
        });
    }

    public override int SaveChanges()
    {
        ApplyTenantId();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantId();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantId()
    {
        // SuperAdmin bypasses tenant isolation
        if (ShouldBypassTenantIsolation())
        {
            // For SuperAdmin, don't enforce tenant ID assignment or validation
            // SuperAdmin can access and modify entities from any tenant
            // But still ensure User entities (SuperAdmin) don't get TenantId
            foreach (var entry in ChangeTracker.Entries<User>())
            {
                if (entry.State == EntityState.Added)
                {
                    // Ensure SuperAdmin users don't get TenantId set
                    var user = entry.Entity;
                    var isSuperAdmin = IsSuperAdmin();
                    if (isSuperAdmin && entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId") is var tenantProp && tenantProp != null)
                    {
                        tenantProp.CurrentValue = null; // SuperAdmin users have no TenantId
                    }
                }
            }
            return;
        }

        var currentTenantId = _tenantProvider.TenantId;
        foreach (var entry in ChangeTracker.Entries())
        {
            // Handle User entity separately - protect against accidental deletion or TenantId modification
            if (entry.Entity is User user)
            {
                // PROTECTION: Log and prevent deletion of users with TenantId set (tenant Admin users)
                // This protects tenant Admin users from accidental deletion during operations
                if (entry.State == EntityState.Deleted)
                {
                    // Protect users that have a TenantId (tenant Admin users)
                    // SuperAdmin users have NULL TenantId and are also protected elsewhere
                    if (!string.IsNullOrEmpty(user.TenantId))
                    {
                        // Log the attempt but allow deletion only if explicitly marked
                        // For now, we'll throw to prevent accidental deletion
                        throw new InvalidOperationException(
                            $"Cannot delete tenant user {user.Email} (TenantId: {user.TenantId}). " +
                            $"Tenant Admin users are protected from deletion. " +
                            $"If you need to delete this user, use the UserManager.DeleteAsync method directly.");
                    }
                }
                
                // PROTECTION: Prevent TenantId modification for users that already have a TenantId
                if (entry.State == EntityState.Modified)
                {
                    var userTenantProp = entry.Property("TenantId");
                    if (userTenantProp != null)
                    {
                        var originalTenantId = userTenantProp.OriginalValue?.ToString();
                        var currentTenantValue = userTenantProp.CurrentValue?.ToString();
                        
                        // Prevent changing TenantId if it was already set (protects tenant Admin users)
                        if (!string.IsNullOrEmpty(originalTenantId) && originalTenantId != currentTenantValue)
                        {
                            throw new InvalidOperationException(
                                $"Cannot modify TenantId for user {user.Email}. " +
                                $"Original TenantId: {originalTenantId}, Attempted TenantId: {currentTenantValue}. " +
                                $"TenantId is protected from modification after user creation.");
                        }
                    }
                }
                
                // Skip setting TenantId on User entities here
                // It will be set via the user creation process
                continue;
            }

            var tenantProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId");
            if (tenantProp != null)
            {
                // For new entities, set the tenant ID
                if (entry.State == EntityState.Added)
                {
                    tenantProp.CurrentValue = currentTenantId;
                }
                // For modified entities, prevent tenant ID changes (critical security check)
                else if (entry.State == EntityState.Modified)
                {
                    var originalTenantId = entry.Property("TenantId").OriginalValue?.ToString();
                    var currentValue = tenantProp.CurrentValue?.ToString();
                    
                    // Prevent tenant ID modification - this is a critical security violation
                    if (!string.IsNullOrEmpty(originalTenantId) && originalTenantId != currentTenantId)
                    {
                        throw new UnauthorizedAccessException(
                            $"Cannot modify entity from tenant '{originalTenantId}' in context of tenant '{currentTenantId}'");
                    }
                    
                    // If someone tries to change the TenantId field, revert it
                    if (originalTenantId != currentValue && !string.IsNullOrEmpty(originalTenantId))
                    {
                        tenantProp.CurrentValue = originalTenantId;
                        throw new InvalidOperationException(
                            "Tenant ID cannot be modified after entity creation for security reasons");
                    }
                }
                // For deleted entities, verify they belong to the current tenant
                else if (entry.State == EntityState.Deleted)
                {
                    var originalTenantId = entry.Property("TenantId").OriginalValue?.ToString();
                    if (!string.IsNullOrEmpty(originalTenantId) && originalTenantId != currentTenantId)
                    {
                        throw new UnauthorizedAccessException(
                            $"Cannot delete entity from tenant '{originalTenantId}' in context of tenant '{currentTenantId}'");
                    }
                }
            }
        }
    }
}
