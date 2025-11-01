using Microsoft.Extensions.Caching.Memory;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Services;

public class TenantMemoryCache : ITenantCache
{
    private readonly IMemoryCache _cache;
    public TenantMemoryCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string ComposeKey(string tenantId, string key) => $"tenant:{tenantId}:{key}";

    public async Task<T> GetOrSetAsync<T>(string tenantId, string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        var composite = ComposeKey(tenantId, key);
        if (_cache.TryGetValue(composite, out T? value) && value is not null)
        {
            return value;
        }
        var created = await factory();
        _cache.Set(composite, created!, ttl);
        return created!;
    }

    public void Remove(string tenantId, string key)
    {
        _cache.Remove(ComposeKey(tenantId, key));
    }
}


