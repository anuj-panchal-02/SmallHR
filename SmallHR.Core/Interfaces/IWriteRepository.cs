namespace SmallHR.Core.Interfaces;

public interface IWriteRepository<T> where T : class
{
	Task<T> AddAsync(T entity);
	Task UpdateAsync(T entity);
	Task DeleteAsync(T entity);
}
