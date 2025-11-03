using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmallHR.Core.DTOs.UsageMetrics;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using SmallHR.Infrastructure.Services;
using System.Text.Json;

namespace SmallHR.Tests.Infrastructure;

public class UsageMetricsServiceDashboardTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ISubscriptionService> _mockSubscriptionService;
    private readonly Mock<ITenantProvider> _mockTenantProvider;
    private readonly ILogger<UsageMetricsService> _logger;
    private readonly UsageMetricsService _service;

    public UsageMetricsServiceDashboardTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _mockTenantProvider = new Mock<ITenantProvider>();
        _mockTenantProvider.Setup(t => t.TenantId).Returns("default");
        
        _context = new ApplicationDbContext(options, _mockTenantProvider.Object);
        
        _mockSubscriptionService = new Mock<ISubscriptionService>();
        
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<UsageMetricsService>();
        
        _service = new UsageMetricsService(
            _context,
            _mockSubscriptionService.Object,
            _logger,
            _mockTenantProvider.Object);
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_ShouldAggregateMetricsAcrossAllTenants()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1);
        
        // Create tenants
        var tenant1 = new Tenant { Id = 1, Name = "Acme Corp", IsActive = true, Status = TenantStatus.Active };
        var tenant2 = new Tenant { Id = 2, Name = "Beta Inc", IsActive = true, Status = TenantStatus.Active };
        _context.Tenants.AddRange(tenant1, tenant2);
        await _context.SaveChangesAsync();

        // Create metrics for tenants
        var metrics1 = new TenantUsageMetrics
        {
            TenantId = 1,
            PeriodStart = periodStart,
            PeriodEnd = periodStart.AddMonths(1).AddDays(-1),
            EmployeeCount = 50,
            UserCount = 20,
            ApiRequestCount = 5000,
            ApiRequestCountToday = 200,
            StorageBytesUsed = 1024 * 1024 * 100 // 100 MB
        };
        
        var metrics2 = new TenantUsageMetrics
        {
            TenantId = 2,
            PeriodStart = periodStart,
            PeriodEnd = periodStart.AddMonths(1).AddDays(-1),
            EmployeeCount = 30,
            UserCount = 15,
            ApiRequestCount = 3000,
            ApiRequestCountToday = 150,
            StorageBytesUsed = 1024 * 1024 * 50 // 50 MB
        };
        
        _context.TenantUsageMetrics.AddRange(metrics1, metrics2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalTenants);
        Assert.Equal(80, result.TotalEmployees); // 50 + 30
        Assert.Equal(8000, result.TotalApiRequests); // 5000 + 3000
        Assert.Equal(350, result.TotalApiRequestsToday); // 200 + 150
        Assert.Equal(1024L * 1024 * 150, result.TotalStorageBytes); // 100 MB + 50 MB
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_ShouldReturnTopTenantsByUsage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1);
        
        var tenants = new[]
        {
            new Tenant { Id = 1, Name = "High Usage", IsActive = true, Status = TenantStatus.Active },
            new Tenant { Id = 2, Name = "Medium Usage", IsActive = true, Status = TenantStatus.Active },
            new Tenant { Id = 3, Name = "Low Usage", IsActive = true, Status = TenantStatus.Active }
        };
        _context.Tenants.AddRange(tenants);
        await _context.SaveChangesAsync();

        var metrics = new[]
        {
            new TenantUsageMetrics
            {
                TenantId = 1,
                PeriodStart = periodStart,
                PeriodEnd = periodStart.AddMonths(1).AddDays(-1),
                EmployeeCount = 100,
                ApiRequestCount = 10000,
                StorageBytesUsed = 1024L * 1024 * 1024 * 5 // 5 GB
            },
            new TenantUsageMetrics
            {
                TenantId = 2,
                PeriodStart = periodStart,
                PeriodEnd = periodStart.AddMonths(1).AddDays(-1),
                EmployeeCount = 50,
                ApiRequestCount = 5000,
                StorageBytesUsed = 1024L * 1024 * 1024 * 2 // 2 GB
            },
            new TenantUsageMetrics
            {
                TenantId = 3,
                PeriodStart = periodStart,
                PeriodEnd = periodStart.AddMonths(1).AddDays(-1),
                EmployeeCount = 10,
                ApiRequestCount = 1000,
                StorageBytesUsed = 1024L * 1024 * 100 // 100 MB
            }
        };
        
        _context.TenantUsageMetrics.AddRange(metrics);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        Assert.NotNull(result.TopTenantsByUsage);
        Assert.True(result.TopTenantsByUsage.Count <= 10);
        Assert.True(result.TopTenantsByUsage.Count >= 3);
        
        // Verify ordering - highest usage should be first
        var topTenant = result.TopTenantsByUsage.First();
        Assert.Equal(1, topTenant.TenantId);
        Assert.Equal("High Usage", topTenant.TenantName);
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_ShouldIncludeAlertsSummaryBySeverity()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1);
        
        var tenant = new Tenant { Id = 1, Name = "Test Tenant", IsActive = true, Status = TenantStatus.Active };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var alerts = new[]
        {
            new Alert { TenantId = 1, AlertType = "Critical", Severity = "Critical", Status = "Active", Message = "Critical alert" },
            new Alert { TenantId = 1, AlertType = "High", Severity = "High", Status = "Active", Message = "High alert" },
            new Alert { TenantId = 1, AlertType = "Medium", Severity = "Medium", Status = "Active", Message = "Medium alert" },
            new Alert { TenantId = 1, AlertType = "Low", Severity = "Low", Status = "Active", Message = "Low alert" },
            new Alert { TenantId = 1, AlertType = "Resolved", Severity = "High", Status = "Resolved", Message = "Resolved alert" }
        };
        
        _context.Alerts.AddRange(alerts);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        Assert.NotNull(result.AlertsSummary);
        Assert.Equal(4, result.AlertsSummary.TotalActive); // Only active alerts
        Assert.Equal(1, result.AlertsSummary.Critical);
        Assert.Equal(1, result.AlertsSummary.High);
        Assert.Equal(1, result.AlertsSummary.Medium);
        Assert.Equal(1, result.AlertsSummary.Low);
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_ShouldInitializeMetricsFromDatabaseWhenMissing()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1);
        
        var tenant = new Tenant { Id = 1, Name = "New Tenant", IsActive = true, Status = TenantStatus.Active };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // Create employees for tenant
        var employees = new[]
        {
            new Employee { TenantId = "1", EmployeeId = "EMP001", FirstName = "John", LastName = "Doe", Email = "john@example.com", Position = "Developer", Department = "IT", IsActive = true },
            new Employee { TenantId = "1", EmployeeId = "EMP002", FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", Position = "Manager", Department = "HR", IsActive = true }
        };
        
        // Set tenant provider to match the tenant for proper TenantId assignment
        _mockTenantProvider.Setup(t => t.TenantId).Returns("1");
        
        _context.Employees.AddRange(employees);
        await _context.SaveChangesAsync();

        // Act - no metrics exist yet
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalEmployees); // Should count from database
        var tenantDashboard = result.Tenants.FirstOrDefault(t => t.TenantId == 1);
        Assert.NotNull(tenantDashboard);
        Assert.Equal(2, tenantDashboard.EmployeeCount);
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_ShouldCalculateTrendsCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var currentPeriodStart = new DateTime(now.Year, now.Month, 1);
        var previousPeriodStart = currentPeriodStart.AddMonths(-1);
        
        var tenant = new Tenant { Id = 1, Name = "Trend Test", IsActive = true, Status = TenantStatus.Active };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var currentMetrics = new TenantUsageMetrics
        {
            TenantId = 1,
            PeriodStart = currentPeriodStart,
            PeriodEnd = currentPeriodStart.AddMonths(1).AddDays(-1),
            EmployeeCount = 100,
            ApiRequestCount = 10000,
            StorageBytesUsed = 1024L * 1024 * 1024 * 10 // 10 GB
        };
        
        var previousMetrics = new TenantUsageMetrics
        {
            TenantId = 1,
            PeriodStart = previousPeriodStart,
            PeriodEnd = previousPeriodStart.AddMonths(1).AddDays(-1),
            EmployeeCount = 80,
            ApiRequestCount = 8000,
            StorageBytesUsed = 1024L * 1024 * 1024 * 8 // 8 GB
        };
        
        _context.TenantUsageMetrics.AddRange(currentMetrics, previousMetrics);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        Assert.NotNull(result.Trend);
        Assert.Equal(100, result.Trend.Employees.CurrentPeriod);
        Assert.Equal(80, result.Trend.Employees.PreviousPeriod);
        Assert.Equal(20, result.Trend.Employees.Change);
        
        Assert.Equal(10000, result.Trend.ApiRequests.CurrentPeriod);
        Assert.Equal(8000, result.Trend.ApiRequests.PreviousPeriod);
        Assert.Equal(2000, result.Trend.ApiRequests.Change);
        
        Assert.Equal(1024L * 1024 * 1024 * 10, result.Trend.Storage.CurrentPeriod);
        Assert.Equal(1024L * 1024 * 1024 * 8, result.Trend.Storage.PreviousPeriod);
    }
}

