namespace SmallHR.Core.DTOs;

/// <summary>
/// Represents a paginated response with metadata
/// </summary>
public class PagedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    
    public int PageNumber { get; set; }
    
    public int PageSize { get; set; }
    
    public int TotalCount { get; set; }
    
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    
    public bool HasPreviousPage => PageNumber > 1;
    
    public bool HasNextPage => PageNumber < TotalPages;
}

