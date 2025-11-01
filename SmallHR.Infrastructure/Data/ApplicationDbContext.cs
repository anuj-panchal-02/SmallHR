using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    private readonly ITenantProvider _tenantProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Position> Positions { get; set; }

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
            entity.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
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
            entity.HasQueryFilter(lr => lr.TenantId == _tenantProvider.TenantId);
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
            entity.HasQueryFilter(a => a.TenantId == _tenantProvider.TenantId);
        });

        // User configuration
        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Address).HasMaxLength(500);
            entity.Property(u => u.City).HasMaxLength(100);
            entity.Property(u => u.State).HasMaxLength(100);
            entity.Property(u => u.ZipCode).HasMaxLength(20);
            entity.Property(u => u.Country).HasMaxLength(100);
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
            entity.HasQueryFilter(rp => rp.TenantId == _tenantProvider.TenantId);
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
            entity.HasQueryFilter(m => m.TenantId == _tenantProvider.TenantId);
        });

        // Tenant configuration
        builder.Entity<Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Domain).HasMaxLength(200);
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
            entity.HasQueryFilter(d => d.TenantId == _tenantProvider.TenantId);
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
            entity.HasQueryFilter(p => p.TenantId == _tenantProvider.TenantId);
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
        var currentTenantId = _tenantProvider.TenantId;
        foreach (var entry in ChangeTracker.Entries())
        {
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
