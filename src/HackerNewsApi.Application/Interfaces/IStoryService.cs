using HackerNewsApi.Application.DTOs;

namespace HackerNewsApi.Application.Interfaces;

/// <summary>
/// Use-case boundary consumed by the API layer (Interface Segregation:
/// controllers only ever need this one, narrow method).
/// </summary>
public interface IStoryService
{
    /// <summary>
    /// Returns up to <paramref name="count"/> stories, ordered by score descending.
    /// </summary>
    Task<IReadOnlyList<StoryDto>> GetBestStoriesAsync(int count, CancellationToken cancellationToken);
}
