using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using System.Security;

namespace SmallHR.Tests.Security;

public class TenantIsolationStrengthTests
{
    private class MockTenantProvider : ITenantProvider
    {
        public MockTenantProvider(string tenantId)
        {
            TenantId = tenantId;
        }
        
        public string TenantId { get; }
    }

    private ApplicationDbContext CreateContext(string tenantId, string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options, new MockTenantProvider(tenantId));
    }

    [Fact]
    public void Should_Throw_When_TenantProvider_Is_Null()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            var context = new ApplicationDbContext(options, null!);
        });
    }

    [Fact]
    public async Task Should_AutoSet_TenantId_On_Create()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext("tenant1", dbName);

        var employee = new Employee
        {
            TenantId = "tenant1",
            EmployeeId = "EMP001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            Department = "Engineering",
            Position = "Developer"
        };

        // Act
        await context.Employees.AddAsync(employee);
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal("tenant1", employee.TenantId);
    }

    [Fact]
    public async Task Should_Prevent_CrossTenant_Modification()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        
        // Create employee in tenant1
        await using (var ctx1 = CreateContext("tenant1", dbName))
        {
            var employee = new Employee
            {
                TenantId = "tenant1",
                EmployeeId = "EMP001",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@test.com",
                Department = "Engineering",
                Position = "Developer"
            };
            await ctx1.Employees.AddAsync(employee);
            await ctx1.SaveChangesAsync();
        }

        // Try to modify from tenant2 context
        await using var ctx2 = CreateContext("tenant2", dbName);
        var employeeFromTenant2 = await ctx2.Employees.FirstOrDefaultAsync();
        
        // Act & Assert
        Assert.Null(employeeFromTenant2); // Query filter prevents seeing it
        
        // Try to get it via IncludeDeleted or bypass filter - should still fail on SaveChanges
        await using var ctx2Force = CreateContext("tenant2", dbName);
        var employees = await ctx2Force.Employees.IgnoreQueryFilters().ToListAsync();
        if (employees.Any())
        {
            var emp = employees.First();
            emp.FirstName = "Hacked";
            
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await ctx2Force.SaveChangesAsync();
            });
        }
    }

    [Fact]
    public async Task Should_Prevent_TenantId_Modification()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext("tenant1", dbName);

        var employee = new Employee
        {
            TenantId = "tenant1",
            EmployeeId = "EMP001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            Department = "Engineering",
            Position = "Developer"
        };
        await context.Employees.AddAsync(employee);
        await context.SaveChangesAsync();

        // Try to change TenantId
        employee.TenantId = "tenant2";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Should_Prevent_CrossTenant_Delete()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        
        // Create employee in tenant1
        await using (var ctx1 = CreateContext("tenant1", dbName))
        {
            var employee = new Employee
            {
                TenantId = "tenant1",
                EmployeeId = "EMP001",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@test.com",
                Department = "Engineering",
                Position = "Developer"
            };
            await ctx1.Employees.AddAsync(employee);
            await ctx1.SaveChangesAsync();
        }

        // Try to delete from tenant2
        await using var ctx2 = CreateContext("tenant2", dbName);
        var employees = await ctx2.Employees.IgnoreQueryFilters().ToListAsync();
        if (employees.Any())
        {
            ctx2.Employees.Remove(employees.First());
            
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await ctx2.SaveChangesAsync();
            });
        }
    }

    [Fact]
    public async Task Should_Prevent_CreatingEntity_From_DifferentTenant()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext("tenant1", dbName);

        var employee = new Employee
        {
            TenantId = "tenant2", // Try to create with different tenant
            EmployeeId = "EMP001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            Department = "Engineering",
            Position = "Developer"
        };

        await context.Employees.AddAsync(employee);

        // Act
        await context.SaveChangesAsync();

        // Assert - Should override with correct tenant
        Assert.Equal("tenant1", employee.TenantId);
    }

    [Fact]
    public async Task Should_Isolate_Departments_By_Tenant()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        
        await using (var ctx1 = CreateContext("tenant1", dbName))
        {
            await ctx1.Departments.AddAsync(new Department
            {
                TenantId = "tenant1",
                Name = "Engineering",
                Description = "Dev team",
                IsActive = true
            });
            await ctx1.SaveChangesAsync();
        }

        await using (var ctx2 = CreateContext("tenant2", dbName))
        {
            await ctx2.Departments.AddAsync(new Department
            {
                TenantId = "tenant2",
                Name = "Engineering",
                Description = "Dev team",
                IsActive = true
            });
            await ctx2.SaveChangesAsync();
        }

        // Act
        await using var queryCtx = CreateContext("tenant1", dbName);
        var departments = await queryCtx.Departments.ToListAsync();

        // Assert
        Assert.Single(departments);
        Assert.Equal("tenant1", departments.First().TenantId);
    }

    [Fact]
    public async Task Should_Isolate_Positions_By_Tenant()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        
        await using (var ctx1 = CreateContext("tenant1", dbName))
        {
            await ctx1.Positions.AddAsync(new Position
            {
                TenantId = "tenant1",
                Title = "Senior Developer",
                Description = "Senior level",
                IsActive = true
            });
            await ctx1.SaveChangesAsync();
        }

        await using (var ctx2 = CreateContext("tenant2", dbName))
        {
            await ctx2.Positions.AddAsync(new Position
            {
                TenantId = "tenant2",
                Title = "Senior Developer",
                Description = "Senior level",
                IsActive = true
            });
            await ctx2.SaveChangesAsync();
        }

        // Act
        await using var queryCtx = CreateContext("tenant1", dbName);
        var positions = await queryCtx.Positions.ToListAsync();

        // Assert
        Assert.Single(positions);
        Assert.Equal("tenant1", positions.First().TenantId);
    }

    [Fact]
    public async Task Should_Isolate_LeaveRequests_By_Tenant()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        
        await using (var ctx1 = CreateContext("tenant1", dbName))
        {
            var emp = new Employee
            {
                TenantId = "tenant1",
                EmployeeId = "EMP001",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@test.com",
                Department = "Engineering",
                Position = "Developer"
            };
            await ctx1.Employees.AddAsync(emp);
            await ctx1.SaveChangesAsync();

            await ctx1.LeaveRequests.AddAsync(new LeaveRequest
            {
                TenantId = "tenant1",
                EmployeeId = emp.Id,
                LeaveType = "Vacation",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(5),
                TotalDays = 5,
                Status = "Pending",
                Reason = "Family trip"
            });
            await ctx1.SaveChangesAsync();
        }

        // Act
        await using var queryCtx = CreateContext("tenant2", dbName);
        var leaveRequests = await queryCtx.LeaveRequests.ToListAsync();

        // Assert
        Assert.Empty(leaveRequests);
    }

    [Fact]
    public async Task Should_Isolate_Attendances_By_Tenant()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        
        await using (var ctx1 = CreateContext("tenant1", dbName))
        {
            var emp = new Employee
            {
                TenantId = "tenant1",
                EmployeeId = "EMP001",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@test.com",
                Department = "Engineering",
                Position = "Developer"
            };
            await ctx1.Employees.AddAsync(emp);
            await ctx1.SaveChangesAsync();

            await ctx1.Attendances.AddAsync(new Attendance
            {
                TenantId = "tenant1",
                EmployeeId = emp.Id,
                Date = DateTime.Today,
                ClockInTime = DateTime.Today.AddHours(9),
                Status = "Present",
                ClockOutTime = DateTime.Today.AddHours(17)
            });
            await ctx1.SaveChangesAsync();
        }

        // Act
        await using var queryCtx = CreateContext("tenant2", dbName);
        var attendances = await queryCtx.Attendances.ToListAsync();

        // Assert
        Assert.Empty(attendances);
    }

    [Fact]
    public async Task Should_Isolate_Modules_By_Tenant()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        
        await using (var ctx1 = CreateContext("tenant1", dbName))
        {
            await ctx1.Modules.AddAsync(new Module
            {
                TenantId = "tenant1",
                Name = "Dashboard",
                Path = "/dashboard",
                DisplayOrder = 1,
                IsActive = true
            });
            await ctx1.SaveChangesAsync();
        }

        await using (var ctx2 = CreateContext("tenant2", dbName))
        {
            await ctx2.Modules.AddAsync(new Module
            {
                TenantId = "tenant2",
                Name = "Reports",
                Path = "/reports",
                DisplayOrder = 1,
                IsActive = true
            });
            await ctx2.SaveChangesAsync();
        }

        // Act
        await using var queryCtx = CreateContext("tenant1", dbName);
        var modules = await queryCtx.Modules.ToListAsync();

        // Assert
        Assert.Single(modules);
        Assert.Equal("tenant1", modules.First().TenantId);
        Assert.Equal("/dashboard", modules.First().Path);
    }

    [Fact]
    public async Task Should_Isolate_RolePermissions_By_Tenant()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        
        await using (var ctx1 = CreateContext("tenant1", dbName))
        {
            await ctx1.RolePermissions.AddAsync(new RolePermission
            {
                TenantId = "tenant1",
                RoleName = "Admin",
                PagePath = "/dashboard",
                PageName = "Dashboard",
                CanAccess = true,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true
            });
            await ctx1.SaveChangesAsync();
        }

        await using (var ctx2 = CreateContext("tenant2", dbName))
        {
            await ctx2.RolePermissions.AddAsync(new RolePermission
            {
                TenantId = "tenant2",
                RoleName = "Admin",
                PagePath = "/reports",
                PageName = "Reports",
                CanAccess = true,
                CanView = true,
                CanCreate = false,
                CanEdit = true,
                CanDelete = false
            });
            await ctx2.SaveChangesAsync();
        }

        // Act
        await using var queryCtx = CreateContext("tenant1", dbName);
        var permissions = await queryCtx.RolePermissions.ToListAsync();

        // Assert
        Assert.Single(permissions);
        Assert.Equal("tenant1", permissions.First().TenantId);
        Assert.Equal("/dashboard", permissions.First().PagePath);
    }

    [Fact]
    public async Task Should_Allow_Modification_Within_Same_Tenant()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext("tenant1", dbName);

        var employee = new Employee
        {
            TenantId = "tenant1",
            EmployeeId = "EMP001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            Department = "Engineering",
            Position = "Developer"
        };
        await context.Employees.AddAsync(employee);
        await context.SaveChangesAsync();

        // Modify within same tenant
        employee.FirstName = "Jane";
        employee.LastName = "Smith";

        // Act
        await context.SaveChangesAsync();

        // Assert - Should succeed
        var updated = await context.Employees.FirstAsync();
        Assert.Equal("Jane", updated.FirstName);
        Assert.Equal("Smith", updated.LastName);
    }
}

