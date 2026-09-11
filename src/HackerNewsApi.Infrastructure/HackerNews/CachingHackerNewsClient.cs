using HackerNewsApi.Application.DTOs;
using HackerNewsApi.Application.Interfaces;
using HackerNewsApi.Infrastructure.Caching;
using HackerNewsApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace HackerNewsApi.Infrastructure.HackerNews;

/// <summary>
/// Decorator (Decorator pattern) that adds caching around any IHackerNewsClient
/// implementation without that implementation needing to know caching exists.
/// This is what lets the API "efficiently service large numbers of requests
/// without overloading the Hacker News API": repeated calls for the same best-
/// story list or the same item are served from memory instead of round-tripping
/// upstream, and MemoryCacheService's per-key locking collapses concurrent
/// cache misses into a single upstream call.
/// Open/Closed: caching was added as a new class, with zero changes to
/// HackerNewsApiClient or to StoryService.
/// </summary>
public sealed class CachingHackerNewsClient : IHackerNewsClient
{
    private const string BestStoryIdsCacheKey = "hn:best-story-ids";
    private static string ItemCacheKey(int id) => $"hn:item:{id}";

    private readonly IHackerNewsClient _inner;
    private readonly ICacheService _cache;
    private readonly HackerNewsOptions _options;

    public CachingHackerNewsClient(
        IHackerNewsClient inner,
        ICacheService cache,
        IOptions<HackerNewsOptions> options)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options.Value;
    }

    public Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync(
            BestStoryIdsCacheKey,
            _options.BestStoryIdsCacheDuration,
            () => _inner.GetBestStoryIdsAsync(cancellationToken));

    public Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync(
            ItemCacheKey(id),
            _options.StoryItemCacheDuration,
            () => _inner.GetItemAsync(id, cancellationToken));
}
