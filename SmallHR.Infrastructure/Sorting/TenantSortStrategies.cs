using System.Linq;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Sorting;

/// <summary>
/// Sort strategies for Tenant entity
/// Each strategy handles sorting by a specific field
/// </summary>
public class TenantNameSortStrategy : ISortStrategy<Tenant>
{
    public string SortField => "name";

    public IQueryable<Tenant> ApplySort(IQueryable<Tenant> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(t => t.Name)
            : query.OrderByDescending(t => t.Name);
    }
}

public class TenantCreatedAtSortStrategy : ISortStrategy<Tenant>
{
    public string SortField => "createdat";

    public IQueryable<Tenant> ApplySort(IQueryable<Tenant> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(t => t.CreatedAt)
            : query.OrderByDescending(t => t.CreatedAt);
    }
}

public class TenantStatusSortStrategy : ISortStrategy<Tenant>
{
    public string SortField => "status";

    public IQueryable<Tenant> ApplySort(IQueryable<Tenant> query, string sortDirection)
    {
        return sortDirection == "asc"
            ? query.OrderBy(t => t.Status)
            : query.OrderByDescending(t => t.Status);
    }
}
