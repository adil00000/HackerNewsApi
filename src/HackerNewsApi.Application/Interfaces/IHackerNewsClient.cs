using HackerNewsApi.Application.DTOs;

namespace HackerNewsApi.Application.Interfaces;

/// <summary>
/// Read-only gateway to the Hacker News API. The Application layer depends on
/// this abstraction only (Dependency Inversion Principle) — it has no knowledge
/// of HttpClient, caching, retries, or throttling, all of which are
/// Infrastructure concerns.
/// </summary>
public interface IHackerNewsClient
{
    /// <summary>Returns the ids of the current "best stories", most-recent first as reported upstream.</summary>
    Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken);

    /// <summary>Returns a single item by id, or null if it does not exist / could not be fetched.</summary>
    Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken);
}
