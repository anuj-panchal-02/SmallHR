using System;
using System.Threading.Tasks;

namespace SmallHR.Core.Interfaces;

public interface ITenantCache
{
    Task<T> GetOrSetAsync<T>(string tenantId, string key, Func<Task<T>> factory, TimeSpan ttl);
    void Remove(string tenantId, string key);
}


