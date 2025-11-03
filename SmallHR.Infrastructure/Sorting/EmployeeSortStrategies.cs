using System.Linq;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Sorting;

/// <summary>
/// Sort strategies for Employee entity
/// Each strategy handles sorting by a specific field
/// </summary>
public class EmployeeFirstNameSortStrategy : ISortStrategy<Employee>
{
    public string SortField => "firstname";

    public IQueryable<Employee> ApplySort(IQueryable<Employee> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(e => e.FirstName)
            : query.OrderByDescending(e => e.FirstName);
    }
}

public class EmployeeLastNameSortStrategy : ISortStrategy<Employee>
{
    public string SortField => "lastname";

    public IQueryable<Employee> ApplySort(IQueryable<Employee> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(e => e.LastName)
            : query.OrderByDescending(e => e.LastName);
    }
}

public class EmployeeEmailSortStrategy : ISortStrategy<Employee>
{
    public string SortField => "email";

    public IQueryable<Employee> ApplySort(IQueryable<Employee> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(e => e.Email)
            : query.OrderByDescending(e => e.Email);
    }
}

public class EmployeeEmployeeIdSortStrategy : ISortStrategy<Employee>
{
    public string SortField => "employeeid";

    public IQueryable<Employee> ApplySort(IQueryable<Employee> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(e => e.EmployeeId)
            : query.OrderByDescending(e => e.EmployeeId);
    }
}

public class EmployeeDepartmentSortStrategy : ISortStrategy<Employee>
{
    public string SortField => "department";

    public IQueryable<Employee> ApplySort(IQueryable<Employee> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(e => e.Department)
            : query.OrderByDescending(e => e.Department);
    }
}

public class EmployeePositionSortStrategy : ISortStrategy<Employee>
{
    public string SortField => "position";

    public IQueryable<Employee> ApplySort(IQueryable<Employee> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(e => e.Position)
            : query.OrderByDescending(e => e.Position);
    }
}

public class EmployeeHireDateSortStrategy : ISortStrategy<Employee>
{
    public string SortField => "hiredate";

    public IQueryable<Employee> ApplySort(IQueryable<Employee> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(e => e.HireDate)
            : query.OrderByDescending(e => e.HireDate);
    }
}

public class EmployeeSalarySortStrategy : ISortStrategy<Employee>
{
    public string SortField => "salary";

    public IQueryable<Employee> ApplySort(IQueryable<Employee> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(e => e.Salary)
            : query.OrderByDescending(e => e.Salary);
    }
}

public class EmployeeCreatedAtSortStrategy : ISortStrategy<Employee>
{
    public string SortField => "createdat";

    public IQueryable<Employee> ApplySort(IQueryable<Employee> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(e => e.CreatedAt)
            : query.OrderByDescending(e => e.CreatedAt);
    }
}
