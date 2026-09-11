using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace HackerNewsApi.Infrastructure.Caching;

/// <summary>
/// IMemoryCache-backed implementation. Uses one SemaphoreSlim per key so that
/// concurrent requests for the same uncached key don't all fall through to the
/// factory at once and hammer the upstream API (a "cache stampede").
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan duration, Func<Task<T>> factory)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync();
        try
        {
            // Re-check: another caller may have populated the cache while we waited.
            if (_cache.TryGetValue(key, out cached) && cached is not null)
            {
                return cached;
            }

            var value = await factory();
            _cache.Set(key, value, duration);
            return value;
        }
        finally
        {
            keyLock.Release();
        }
    }
}
