namespace SmallHR.Core.Interfaces;

public interface IBulkWriteRepository<T> where T : class
{
	Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
	Task DeleteRangeAsync(IEnumerable<T> entities);
}
