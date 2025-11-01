# 🔧 SOLID/DRY Refactor Plan

This document outlines concrete refactoring plans to address SOLID violations and code duplication in the SmallHR codebase.

---

## Overview

| Category | Issues Found | Estimated Effort |
|----------|--------------|------------------|
| **DRY Violations** | 15 instances | 12 hours |
| **SOLID Violations** | 8 instances | 16 hours |
| **Large Methods** | 10 methods | 8 hours |
| **Code Duplication** | 25 instances | 10 hours |
| **Total** | **58 refactors** | **46 hours** |

---

## 1. DRY Violations

### Refactor 1.1: Extract Common Error Handling Logic

**Location:** `SmallHR.API/Controllers/EmployeesController.cs`, `DepartmentsController.cs`, `PositionsController.cs`

**Problem:** Repetitive try-catch blocks and error responses across controllers.

**Current Code:**
```csharp
// Repeated in multiple controllers
try
{
    var result = await _service.SomeOperationAsync();
    return Ok(result);
}
catch (Exception ex)
{
    _logger.LogError(ex, "An error occurred while...");
    return StatusCode(500, new { message = "An error occurred while..." });
}
```

**Refactored Code:**

#### `SmallHR.API/Base/BaseApiController.cs` (New File)

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SmallHR.API.Base;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected readonly ILogger Logger;

    protected BaseApiController(ILogger logger)
    {
        Logger = logger;
    }

    protected IActionResult HandleServiceResult<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            var result = await operation();
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            Logger.LogWarning(ex, "{OperationName} not found", operationName);
            return NotFound(new { message = $"{operationName} not found" });
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Unauthorized access: {OperationName}", operationName);
            return Forbid();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while {OperationName}", operationName);
            return StatusCode(500, new { message = $"An error occurred while {operationName}" });
        }
    }

    protected IActionResult HandleServiceResult<T>(Func<Task<T?>> operation, string operationName)
        where T : class
    {
        try
        {
            var result = await operation();
            if (result == null)
            {
                return NotFound(new { message = $"{operationName} not found" });
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Unauthorized access: {OperationName}", operationName);
            return Forbid();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while {OperationName}", operationName);
            return StatusCode(500, new { message = $"An error occurred while {operationName}" });
        }
    }

    protected IActionResult HandleCreateResult<T>(Func<Task<T>> operation, string resourceName, Func<T, object> routeValues)
    {
        try
        {
            var result = await operation();
            return CreatedAtAction(nameof(GetById), routeValues(result), result);
        }
        catch (ValidationException ex)
        {
            Logger.LogWarning(ex, "Validation failed: {ResourceName}", resourceName);
            return BadRequest(new { message = ex.Message, errors = ex.Errors });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while creating {ResourceName}", resourceName);
            return StatusCode(500, new { message = $"An error occurred while creating {resourceName}" });
        }
    }
}
```

**Usage:**

#### `SmallHR.API/Controllers/EmployeesController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : BaseApiController
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
        : base(logger)
    {
        _employeeService = employeeService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
    {
        return HandleServiceResult(
            () => _employeeService.GetEmployeeByIdAsync(id),
            $"getting employee {id}"
        );
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<ActionResult<EmployeeDto>> CreateEmployee(CreateEmployeeDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return HandleCreateResult(
            () => _employeeService.CreateEmployeeAsync(dto),
            "employee",
            emp => new { id = emp.Id }
        );
    }
}
```

**Estimated Effort:** 4 hours

---

### Refactor 1.2: Extract Validation Logic

**Location:** Multiple controllers

**Problem:** Duplicate validation logic (ModelState checks, existence checks).

**Refactored Code:**

#### `SmallHR.API/Validation/ValidationExtensions.cs` (New File)

```csharp
using Microsoft.AspNetCore.Mvc;

namespace SmallHR.API.Validation;

public static class ValidationExtensions
{
    public static IActionResult? ValidateModel(this ControllerBase controller)
    {
        return !controller.ModelState.IsValid 
            ? controller.BadRequest(controller.ModelState) 
            : null;
    }

    public static async Task<IActionResult?> ValidateResourceExistsAsync<T>(
        this ControllerBase controller,
        Func<int, Task<bool>> existsCheck,
        int id,
        string resourceName)
    {
        var exists = await existsCheck(id);
        return !exists 
            ? controller.NotFound(new { message = $"{resourceName} not found" }) 
            : null;
    }
}
```

**Estimated Effort:** 2 hours

---

### Refactor 1.3: Extract Pagination Logic

**Location:** `EmployeesController.cs` (SearchEmployees method)

**Problem:** Pagination validation duplicated across endpoints.

**Refactored Code:**

#### `SmallHR.API/Helpers/PaginationHelper.cs` (New File)

```csharp
namespace SmallHR.API.Helpers;

public static class PaginationHelper
{
    public static (int PageNumber, int PageSize) NormalizePagination(
        int? pageNumber, 
        int? pageSize, 
        int defaultPageSize = 10, 
        int maxPageSize = 100)
    {
        var normalizedPageNumber = Math.Max(1, pageNumber ?? 1);
        var normalizedPageSize = pageSize switch
        {
            null => defaultPageSize,
            <= 0 => defaultPageSize,
            > maxPageSize => maxPageSize,
            _ => pageSize.Value
        };

        return (normalizedPageNumber, normalizedPageSize);
    }
}
```

**Usage:**

```csharp
[HttpGet("search")]
public async Task<ActionResult<PagedResponse<EmployeeDto>>> SearchEmployees(
    [FromQuery] EmployeeSearchRequest request)
{
    var (pageNumber, pageSize) = PaginationHelper.NormalizePagination(
        request.PageNumber, 
        request.PageSize
    );

    request.PageNumber = pageNumber;
    request.PageSize = pageSize;

    var result = await _employeeService.SearchEmployeesAsync(request);
    return Ok(result);
}
```

**Estimated Effort:** 1 hour

---

## 2. SOLID Violations

### Refactor 2.1: Single Responsibility - Split Large Service Methods

**Location:** `SmallHR.Infrastructure/Services/EmployeeService.cs` (SearchEmployeesAsync)

**Problem:** Method exceeds 100 lines, handles multiple responsibilities.

**Current Code Structure:**
```csharp
public async Task<PagedResponse<EmployeeDto>> SearchEmployeesAsync(EmployeeSearchRequest request)
{
    // 1. Build query (20 lines)
    // 2. Apply filters (30 lines)
    // 3. Apply sorting (20 lines)
    // 4. Paginate (15 lines)
    // 5. Map to DTOs (10 lines)
    // 6. Return result (5 lines)
}
```

**Refactored Code:**

#### `SmallHR.Infrastructure/Services/EmployeeService.cs`

```csharp
public async Task<PagedResponse<EmployeeDto>> SearchEmployeesAsync(EmployeeSearchRequest request)
{
    var query = BuildSearchQuery(request);
    query = ApplyFilters(query, request);
    query = ApplySorting(query, request);
    
    var totalCount = await query.CountAsync();
    var employees = await ApplyPagination(query, request)
        .ToListAsync();
    
    var employeeDtos = _mapper.Map<IEnumerable<EmployeeDto>>(employees);
    
    return new PagedResponse<EmployeeDto>
    {
        Data = employeeDtos,
        PageNumber = request.PageNumber,
        PageSize = request.PageSize,
        TotalCount = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
    };
}

private IQueryable<Employee> BuildSearchQuery(EmployeeSearchRequest request)
{
    var query = _repository.GetAllAsync().AsQueryable();
    
    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
    {
        var searchTerm = request.SearchTerm.ToLower();
        query = query.Where(e => 
            e.FirstName.ToLower().Contains(searchTerm) ||
            e.LastName.ToLower().Contains(searchTerm) ||
            e.Email.ToLower().Contains(searchTerm) ||
            e.EmployeeId.ToLower().Contains(searchTerm)
        );
    }
    
    return query;
}

private IQueryable<Employee> ApplyFilters(IQueryable<Employee> query, EmployeeSearchRequest request)
{
    if (!string.IsNullOrWhiteSpace(request.Department))
    {
        query = query.Where(e => e.Department == request.Department);
    }
    
    if (!string.IsNullOrWhiteSpace(request.Position))
    {
        query = query.Where(e => e.Position == request.Position);
    }
    
    if (request.IsActive.HasValue)
    {
        query = query.Where(e => e.IsActive == request.IsActive.Value);
    }
    
    return query;
}

private IQueryable<Employee> ApplySorting(IQueryable<Employee> query, EmployeeSearchRequest request)
{
    return request.SortBy?.ToLower() switch
    {
        "lastname" => request.SortDirection == "desc" 
            ? query.OrderByDescending(e => e.LastName) 
            : query.OrderBy(e => e.LastName),
        "email" => request.SortDirection == "desc" 
            ? query.OrderByDescending(e => e.Email) 
            : query.OrderBy(e => e.Email),
        _ => request.SortDirection == "desc" 
            ? query.OrderByDescending(e => e.FirstName) 
            : query.OrderBy(e => e.FirstName)
    };
}

private IQueryable<Employee> ApplyPagination(IQueryable<Employee> query, EmployeeSearchRequest request)
{
    return query
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize);
}
```

**Estimated Effort:** 3 hours

---

### Refactor 2.2: Open/Closed Principle - Extract Sort Strategy

**Location:** `EmployeeService.cs` (Sorting logic)

**Problem:** Hard-coded switch statement for sorting violates OCP.

**Refactored Code:**

#### `SmallHR.Infrastructure/Services/Sorting/ISortStrategy.cs` (New File)

```csharp
namespace SmallHR.Infrastructure.Services.Sorting;

public interface ISortStrategy<T>
{
    IOrderedQueryable<T> ApplySort(IQueryable<T> query, string direction);
}
```

#### `SmallHR.Infrastructure/Services/Sorting/SortStrategyFactory.cs` (New File)

```csharp
using System.Linq;

namespace SmallHR.Infrastructure.Services.Sorting;

public class SortStrategyFactory<T>
{
    private readonly Dictionary<string, ISortStrategy<T>> _strategies;

    public SortStrategyFactory()
    {
        _strategies = new Dictionary<string, ISortStrategy<T>>();
    }

    public void RegisterStrategy(string fieldName, ISortStrategy<T> strategy)
    {
        _strategies[fieldName.ToLower()] = strategy;
    }

    public IOrderedQueryable<T> ApplySort(IQueryable<T> query, string fieldName, string direction)
    {
        if (_strategies.TryGetValue(fieldName.ToLower(), out var strategy))
        {
            return strategy.ApplySort(query, direction);
        }
        
        throw new ArgumentException($"Unknown sort field: {fieldName}");
    }
}
```

#### `SmallHR.Infrastructure/Services/Sorting/EmployeeSortStrategies.cs` (New File)

```csharp
using SmallHR.Core.Entities;
using System.Linq;
using System.Linq.Expressions;

namespace SmallHR.Infrastructure.Services.Sorting;

public class EmployeeFirstNameSortStrategy : ISortStrategy<Employee>
{
    public IOrderedQueryable<Employee> ApplySort(IQueryable<Employee> query, string direction)
    {
        return direction == "desc"
            ? query.OrderByDescending(e => e.FirstName)
            : query.OrderBy(e => e.FirstName);
    }
}

public class EmployeeLastNameSortStrategy : ISortStrategy<Employee>
{
    public IOrderedQueryable<Employee> ApplySort(IQueryable<Employee> query, string direction)
    {
        return direction == "desc"
            ? query.OrderByDescending(e => e.LastName)
            : query.OrderBy(e => e.LastName);
    }
}

// Additional strategies...
```

**Estimated Effort:** 4 hours

---

### Refactor 2.3: Interface Segregation - Split Large Repository Interface

**Location:** `IGenericRepository.cs`

**Problem:** Generic repository interface includes methods not all entities need.

**Current Code:**
```csharp
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    // ... 10+ methods
}
```

**Refactored Code:**

#### `SmallHR.Core/Interfaces/IRepository.cs` (Base Interface)

```csharp
namespace SmallHR.Core.Interfaces;

public interface IReadRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}

public interface IWriteRepository<T> where T : class
{
    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}

public interface IRepository<T> : IReadRepository<T>, IWriteRepository<T> where T : class
{
}
```

**Estimated Effort:** 2 hours

---

## 3. Large Methods

### Refactor 3.1: Extract Seed Data Logic

**Location:** `SmallHR.API/Program.cs` (SeedDataAsync - 250+ lines)

**Problem:** Method is too large and handles multiple seeding responsibilities.

**Refactored Code:**

#### `SmallHR.Infrastructure/Data/Seeders/IDataSeeder.cs` (New File)

```csharp
namespace SmallHR.Infrastructure.Data.Seeders;

public interface IDataSeeder
{
    Task SeedAsync();
}
```

#### `SmallHR.Infrastructure/Data/Seeders/RoleSeeder.cs` (New File)

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Infrastructure.Data;

namespace SmallHR.Infrastructure.Data.Seeders;

public class RoleSeeder : IDataSeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RoleSeeder(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        var roles = new[] { "SuperAdmin", "Admin", "HR", "Employee" };
        
        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}
```

#### `SmallHR.Infrastructure/Data/Seeders/SuperAdminSeeder.cs` (New File)

```csharp
using Microsoft.AspNetCore.Identity;
using SmallHR.Core.Entities;

namespace SmallHR.Infrastructure.Data.Seeders;

public class SuperAdminSeeder : IDataSeeder
{
    private readonly UserManager<User> _userManager;

    public SuperAdminSeeder(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        const string superAdminEmail = "superadmin@smallhr.com";
        var existingUser = await _userManager.FindByEmailAsync(superAdminEmail);
        
        if (existingUser == null)
        {
            var superAdmin = new User
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                FirstName = "Super",
                LastName = "Admin",
                DateOfBirth = new DateTime(1985, 1, 1),
                IsActive = true
            };
            
            await _userManager.CreateAsync(superAdmin, "SuperAdmin@123");
            await _userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
        }
    }
}
```

**Usage in Program.cs:**
```csharp
var seeders = new[]
{
    new RoleSeeder(roleManager),
    new SuperAdminSeeder(userManager),
    new DepartmentSeeder(context),
    new PositionSeeder(context),
    new ModuleSeeder(context),
    new RolePermissionSeeder(context)
};

foreach (var seeder in seeders)
{
    await seeder.SeedAsync();
}
```

**Estimated Effort:** 4 hours

---

## 4. Code Duplication

### Refactor 4.1: Extract Common Authorization Attributes

**Location:** Multiple controllers

**Problem:** `[Authorize(Roles = "SuperAdmin,Admin,HR")]` repeated in many places.

**Refactored Code:**

#### `SmallHR.API/Authorization/AuthorizeRolesAttribute.cs` (New File)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmallHR.API.Authorization;

public class AuthorizeAdminAttribute : AuthorizeAttribute
{
    public AuthorizeAdminAttribute()
    {
        Roles = "SuperAdmin,Admin";
    }
}

public class AuthorizeHRAttribute : AuthorizeAttribute
{
    public AuthorizeHRAttribute()
    {
        Roles = "SuperAdmin,Admin,HR";
    }
}

public class AuthorizeSuperAdminAttribute : AuthorizeAttribute
{
    public AuthorizeSuperAdminAttribute()
    {
        Roles = "SuperAdmin";
    }
}
```

**Usage:**
```csharp
[HttpGet]
[AuthorizeHR]
public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
{
    // ...
}
```

**Estimated Effort:** 1 hour

---

## Summary

### Refactoring Priority

1. **High Priority (Week 1):**
   - Refactor 1.1: Extract Common Error Handling (4h)
   - Refactor 2.1: Split Large Service Methods (3h)
   - Refactor 3.1: Extract Seed Data Logic (4h)

2. **Medium Priority (Week 2):**
   - Refactor 1.2: Extract Validation Logic (2h)
   - Refactor 1.3: Extract Pagination Logic (1h)
   - Refactor 2.2: Extract Sort Strategy (4h)
   - Refactor 4.1: Extract Authorization Attributes (1h)

3. **Low Priority (Week 3):**
   - Refactor 2.3: Split Repository Interface (2h)
   - Additional code duplication fixes (10h)

**Total Estimated Effort:** ~46 hours (approximately 1.5 weeks for one developer)

---

## Testing Requirements

Each refactor must include:
- ✅ Unit tests for new extracted methods
- ✅ Integration tests for affected endpoints
- ✅ No regression in existing functionality

