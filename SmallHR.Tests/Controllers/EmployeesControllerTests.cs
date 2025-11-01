using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmallHR.API.Controllers;
using SmallHR.Core.DTOs.Employee;
using SmallHR.Core.Interfaces;

namespace SmallHR.Tests.Controllers;

public class EmployeesControllerTests
{
    private readonly Mock<IEmployeeService> _mockService;
    private readonly Mock<ILogger<EmployeesController>> _mockLogger;
    private readonly EmployeesController _controller;

    public EmployeesControllerTests()
    {
        _mockService = new Mock<IEmployeeService>();
        _mockLogger = new Mock<ILogger<EmployeesController>>();
        _controller = new EmployeesController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnOkResult()
    {
        // Arrange
        var employees = new List<EmployeeDto>
        {
            new EmployeeDto { Id = 1, EmployeeId = "EMP001", FirstName = "John", LastName = "Doe" },
            new EmployeeDto { Id = 2, EmployeeId = "EMP002", FirstName = "Jane", LastName = "Smith" }
        };
        
        _mockService.Setup(s => s.GetAllEmployeesAsync()).ReturnsAsync(employees);

        // Act
        var result = await _controller.GetEmployees();

        // Assert
        var okResult = Assert.IsType<ActionResult<IEnumerable<EmployeeDto>>>(result);
        var actionResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var returnedEmployees = Assert.IsAssignableFrom<IEnumerable<EmployeeDto>>(actionResult.Value);
        Assert.Equal(2, returnedEmployees.Count());
    }

    [Fact]
    public async Task GetEmployee_WithValidId_ShouldReturnEmployee()
    {
        // Arrange
        var employee = new EmployeeDto { Id = 1, EmployeeId = "EMP001", FirstName = "John", LastName = "Doe" };
        _mockService.Setup(s => s.GetEmployeeByIdAsync(1)).ReturnsAsync(employee);

        // Act
        var result = await _controller.GetEmployee(1);

        // Assert
        var okResult = Assert.IsType<ActionResult<EmployeeDto>>(result);
        var actionResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var returnedEmployee = Assert.IsType<EmployeeDto>(actionResult.Value);
        Assert.Equal("EMP001", returnedEmployee.EmployeeId);
    }

    [Fact]
    public async Task GetEmployee_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetEmployeeByIdAsync(999)).ReturnsAsync((EmployeeDto?)null);

        // Act
        var result = await _controller.GetEmployee(999);

        // Assert
        var notFoundResult = Assert.IsType<ActionResult<EmployeeDto>>(result);
        Assert.IsType<NotFoundResult>(notFoundResult.Result);
    }

    [Fact]
    public async Task CreateEmployee_WithValidData_ShouldReturnCreated()
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

        var employee = new EmployeeDto
        {
            Id = 3,
            EmployeeId = createDto.EmployeeId,
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Email = createDto.Email
        };

        _mockService.Setup(s => s.EmployeeIdExistsAsync(createDto.EmployeeId)).ReturnsAsync(false);
        _mockService.Setup(s => s.EmailExistsAsync(createDto.Email)).ReturnsAsync(false);
        _mockService.Setup(s => s.CreateEmployeeAsync(createDto)).ReturnsAsync(employee);

        // Act
        var result = await _controller.CreateEmployee(createDto);

        // Assert
        var createdResult = Assert.IsType<ActionResult<EmployeeDto>>(result);
        var actionResult = Assert.IsType<CreatedAtActionResult>(createdResult.Result);
        var returnedEmployee = Assert.IsType<EmployeeDto>(actionResult.Value);
        Assert.Equal("EMP003", returnedEmployee.EmployeeId);
    }

    [Fact]
    public async Task CreateEmployee_WithExistingEmployeeId_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreateEmployeeDto
        {
            EmployeeId = "EMP001",
            FirstName = "Bob",
            LastName = "Johnson",
            Email = "bob@example.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            HireDate = DateTime.Now,
            Position = "Developer",
            Department = "IT",
            Salary = 50000
        };

        _mockService.Setup(s => s.EmployeeIdExistsAsync(createDto.EmployeeId)).ReturnsAsync(true);

        // Act
        var result = await _controller.CreateEmployee(createDto);

        // Assert
        var badRequestResult = Assert.IsType<ActionResult<EmployeeDto>>(result);
        var actionResult = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        Assert.Contains("Employee ID already exists", actionResult.Value?.ToString());
    }

    [Fact]
    public async Task DeleteEmployee_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        _mockService.Setup(s => s.EmployeeExistsAsync(1)).ReturnsAsync(true);
        _mockService.Setup(s => s.DeleteEmployeeAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteEmployee(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteEmployee_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.EmployeeExistsAsync(999)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteEmployee(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
