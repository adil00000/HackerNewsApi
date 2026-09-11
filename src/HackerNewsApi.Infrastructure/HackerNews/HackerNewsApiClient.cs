using System.Net.Http.Json;
using HackerNewsApi.Application.DTOs;
using HackerNewsApi.Application.Interfaces;
using HackerNewsApi.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HackerNewsApi.Infrastructure.HackerNews;

/// <summary>
/// Talks to the real Hacker News API over HTTP.
/// Single Responsibility: this class knows only how to make the two upstream
/// calls and how to avoid hammering the upstream service by bounding
/// concurrency with a semaphore. Retries/circuit-breaking are configured on the
/// HttpClient itself via Polly (see ServiceCollectionExtensions), so this class
/// doesn't need to know about resilience policies either.
/// Caching is added transparently by CachingHackerNewsClient, which decorates
/// this class (Decorator pattern) — this class stays unaware caching exists.
/// </summary>
public sealed class HackerNewsApiClient : IHackerNewsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HackerNewsApiClient> _logger;
    private readonly SemaphoreSlim _throttle;

    public HackerNewsApiClient(
        HttpClient httpClient,
        IOptions<HackerNewsOptions> options,
        ILogger<HackerNewsApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var maxConcurrency = Math.Max(1, options.Value.MaxConcurrentUpstreamRequests);
        _throttle = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        await _throttle.WaitAsync(cancellationToken);
        try
        {
            var ids = await _httpClient.GetFromJsonAsync<int[]>("beststories.json", cancellationToken);
            return ids ?? Array.Empty<int>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Failed to retrieve best story ids from Hacker News.");
            return Array.Empty<int>();
        }
        finally
        {
            _throttle.Release();
        }
    }

    public async Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        await _throttle.WaitAsync(cancellationToken);
        try
        {
            return await _httpClient.GetFromJsonAsync<HackerNewsItem>($"item/{id}.json", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to retrieve item {ItemId} from Hacker News.", id);
            return null;
        }
        finally
        {
            _throttle.Release();
        }
    }
}
