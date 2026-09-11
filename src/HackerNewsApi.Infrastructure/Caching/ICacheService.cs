namespace HackerNewsApi.Infrastructure.Caching;

/// <summary>
/// Narrow caching abstraction. Keeping this separate from IMemoryCache means
/// the caching decorator depends on an interface it fully uses (Interface
/// Segregation) and the backing store could later be swapped for a
/// distributed cache (e.g. Redis) without touching consumers.
/// </summary>
public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string key, TimeSpan duration, Func<Task<T>> factory);
}
