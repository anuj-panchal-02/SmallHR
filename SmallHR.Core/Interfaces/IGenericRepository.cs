using System.Linq.Expressions;

namespace SmallHR.Core.Interfaces;

public interface IGenericRepository<T> : IReadRepository<T>, IWriteRepository<T>, IBulkWriteRepository<T> where T : class
{
}
