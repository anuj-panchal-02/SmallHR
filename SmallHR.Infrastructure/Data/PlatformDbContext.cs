using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Data;

/// <summary>
/// Platform-level DbContext for SuperAdmin operations
/// Provides access to all tenants' data without tenant isolation
/// </summary>
public class PlatformDbContext : DbContext
{
    private readonly string _connectionString;

    public PlatformDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure entities without tenant query filters
        // SuperAdmin can access all tenants' data
        
        // Employee configuration (no query filter)
        builder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.Email);
        });

        // LeaveRequest configuration (no query filter)
        builder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(lr => lr.Id);
            entity.HasIndex(lr => lr.TenantId);
        });

        // Attendance configuration (no query filter)
        builder.Entity<Attendance>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.TenantId);
        });

        // Add other entities as needed for platform-level access
    }

    // Platform-level DbSets (access all tenants' data)
    public DbSet<Employee> Employees { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<TenantUsageMetrics> TenantUsageMetrics { get; set; }
    public DbSet<TenantLifecycleEvent> TenantLifecycleEvents { get; set; }
}

