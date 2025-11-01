using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmallHR.Core.DTOs.Employee;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using SmallHR.Infrastructure.Mapping;
using SmallHR.Infrastructure.Services;

namespace SmallHR.Tests.Services;

public class EmployeeServiceTests
{
    private readonly Mock<IEmployeeRepository> _mockRepository;
    private readonly IMapper _mapper;
    private readonly EmployeeService _service;
    private readonly Mock<ITenantProvider> _mockTenantProvider;

    public EmployeeServiceTests()
    {
        _mockRepository = new Mock<IEmployeeRepository>();
        _mockTenantProvider = new Mock<ITenantProvider>();
        _mockTenantProvider.Setup(t => t.TenantId).Returns("default");
        
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        
        _service = new EmployeeService(_mockRepository.Object, _mapper, _mockTenantProvider.Object);
    }

    [Fact]
    public async Task GetAllEmployeesAsync_ShouldReturnAllEmployees()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee { TenantId = "default", Id = 1, EmployeeId = "EMP001", FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            new Employee { TenantId = "default", Id = 2, EmployeeId = "EMP002", FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
        };
        
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(employees);

        // Act
        var result = await _service.GetAllEmployeesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, e => e.EmployeeId == "EMP001");
        Assert.Contains(result, e => e.EmployeeId == "EMP002");
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_WithValidId_ShouldReturnEmployee()
    {
        // Arrange
        var employee = new Employee { TenantId = "default", Id = 1, EmployeeId = "EMP001", FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);

        // Act
        var result = await _service.GetEmployeeByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EMP001", result.EmployeeId);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Employee?)null);

        // Act
        var result = await _service.GetEmployeeByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithValidData_ShouldCreateEmployee()
    {
        // Arrange
        var createDto = new CreateEmployeeDto
        {
            EmployeeId = "EMP003",
            FirstName = "Bob",
            LastName = "Johnson",
            Email = "bob@example.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            HireDate = DateTime.Now,
            Position = "Developer",
            Department = "IT",
            Salary = 50000
        };

        var employee = new Employee
        {
            TenantId = "default",
            Id = 3,
            EmployeeId = createDto.EmployeeId,
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Email = createDto.Email,
            DateOfBirth = createDto.DateOfBirth,
            HireDate = createDto.HireDate,
            Position = createDto.Position,
            Department = createDto.Department,
            Salary = createDto.Salary
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Employee>())).ReturnsAsync(employee);

        // Act
        var result = await _service.CreateEmployeeAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EMP003", result.EmployeeId);
        Assert.Equal("Bob", result.FirstName);
        Assert.Equal("Johnson", result.LastName);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Employee>()), Times.Once);
    }

    [Fact]
    public async Task EmployeeExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, bool>>>()))
                      .ReturnsAsync(true);

        // Act
        var result = await _service.EmployeeExistsAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EmployeeExistsAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, bool>>>()))
                      .ReturnsAsync(false);

        // Act
        var result = await _service.EmployeeExistsAsync(999);

        // Assert
        Assert.False(result);
    }
}
