using System.Collections.Generic;

namespace SmallHR.Core.Interfaces;

/// <summary>
/// Factory for retrieving sort strategies
/// Follows Open/Closed Principle - new strategies can be registered without modifying the factory
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface ISortStrategyFactory<T>
{
    /// <summary>
    /// Gets a sort strategy for the specified sort field
    /// </summary>
    /// <param name="sortField">The field name to sort by</param>
    /// <returns>The sort strategy, or null if not found</returns>
    ISortStrategy<T>? GetStrategy(string sortField);

    /// <summary>
    /// Gets the default sort strategy
    /// </summary>
    /// <returns>The default sort strategy</returns>
    ISortStrategy<T> GetDefaultStrategy();

    /// <summary>
    /// Gets all available sort strategies
    /// </summary>
    /// <returns>List of available sort strategies</returns>
    IReadOnlyList<ISortStrategy<T>> GetAllStrategies();
}
