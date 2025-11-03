using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Sorting;

/// <summary>
/// Factory implementation for Employee sort strategies
/// </summary>
public class EmployeeSortStrategyFactory : ISortStrategyFactory<Employee>
{
    private readonly Dictionary<string, ISortStrategy<Employee>> _strategies;
    private readonly ISortStrategy<Employee> _defaultStrategy;

    public EmployeeSortStrategyFactory(IEnumerable<ISortStrategy<Employee>> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.SortField.ToLowerInvariant());
        _defaultStrategy = _strategies.GetValueOrDefault("firstname") ?? _strategies.Values.First();
    }

    public ISortStrategy<Employee>? GetStrategy(string sortField)
    {
        if (string.IsNullOrWhiteSpace(sortField))
            return _defaultStrategy;

        return _strategies.GetValueOrDefault(sortField.ToLowerInvariant());
    }

    public ISortStrategy<Employee> GetDefaultStrategy()
    {
        return _defaultStrategy;
    }

    public IReadOnlyList<ISortStrategy<Employee>> GetAllStrategies()
    {
        return _strategies.Values.ToList();
    }
}

/// <summary>
/// Factory implementation for Tenant sort strategies
/// </summary>
public class TenantSortStrategyFactory : ISortStrategyFactory<Tenant>
{
    private readonly Dictionary<string, ISortStrategy<Tenant>> _strategies;
    private readonly ISortStrategy<Tenant> _defaultStrategy;

    public TenantSortStrategyFactory(IEnumerable<ISortStrategy<Tenant>> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.SortField.ToLowerInvariant());
        _defaultStrategy = _strategies.GetValueOrDefault("createdat") ?? _strategies.Values.First();
    }

    public ISortStrategy<Tenant>? GetStrategy(string sortField)
    {
        if (string.IsNullOrWhiteSpace(sortField))
            return _defaultStrategy;

        return _strategies.GetValueOrDefault(sortField.ToLowerInvariant());
    }

    public ISortStrategy<Tenant> GetDefaultStrategy()
    {
        return _defaultStrategy;
    }

    public IReadOnlyList<ISortStrategy<Tenant>> GetAllStrategies()
    {
        return _strategies.Values.ToList();
    }
}
