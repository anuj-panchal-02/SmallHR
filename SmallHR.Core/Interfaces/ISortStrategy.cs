namespace SmallHR.Core.Interfaces;

/// <summary>
/// Strategy pattern interface for sorting entities
/// Follows Open/Closed Principle - new sorting strategies can be added without modifying existing code
/// </summary>
/// <typeparam name="T">The entity type to sort</typeparam>
public interface ISortStrategy<T>
{
    /// <summary>
    /// Gets the sort field name that this strategy handles
    /// </summary>
    string SortField { get; }

    /// <summary>
    /// Applies sorting to the queryable collection
    /// </summary>
    /// <param name="query">The queryable collection</param>
    /// <param name="sortDirection">Sort direction: "asc" or "desc"</param>
    /// <returns>Ordered queryable collection</returns>
    IQueryable<T> ApplySort(IQueryable<T> query, string sortDirection);
}
