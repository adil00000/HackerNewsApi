using HackerNewsApi.Application.DTOs;
using HackerNewsApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HackerNewsApi.Application.Services;

/// <summary>
/// Orchestrates retrieval of the best n Hacker News stories.
/// Single Responsibility: this class only knows how to turn "give me n stories"
/// into a correctly sorted, correctly shaped result — it has zero knowledge of
/// HTTP, caching, or retry policies (those live behind IHackerNewsClient).
/// </summary>
public sealed class StoryService : IStoryService
{
    private readonly IHackerNewsClient _client;
    private readonly ILogger<StoryService> _logger;

    public StoryService(IHackerNewsClient client, ILogger<StoryService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<StoryDto>> GetBestStoriesAsync(int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "count must be a positive integer.");
        }

        IReadOnlyList<int> ids = await _client.GetBestStoryIdsAsync(cancellationToken);

        if (ids.Count == 0)
        {
            _logger.LogWarning("Hacker News returned no best story ids.");
            return Array.Empty<StoryDto>();
        }

        // We can't assume beststories.json is itself sorted by score, so we fetch
        // details for every candidate id and sort locally. Fetching is delegated
        // to IHackerNewsClient, whose implementation is responsible for doing this
        // efficiently (caching + bounded concurrency) so this layer stays simple.
        var fetchTasks = ids.Select(id => _client.GetItemAsync(id, cancellationToken));
        HackerNewsItem?[] items = await Task.WhenAll(fetchTasks);

        var stories = items
            .Where(IsPublishableStory)
            .Select(ToDto!)
            .OrderByDescending(s => s.Score)
            .Take(count)
            .ToList();

        return stories;
    }

    private static bool IsPublishableStory(HackerNewsItem? item) =>
        item is { Deleted: false, Dead: false } &&
        !string.IsNullOrWhiteSpace(item.Title);

    private static StoryDto ToDto(HackerNewsItem item) => new(
        Title: item.Title!,
        Uri: item.Url ?? string.Empty,
        PostedBy: item.By ?? string.Empty,
        Time: DateTimeOffset.FromUnixTimeSeconds(item.Time),
        Score: item.Score,
        CommentCount: item.Descendants);
}
