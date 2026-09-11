namespace HackerNewsApi.Application.DTOs;

/// <summary>
/// The public shape of a story returned to API consumers.
/// Deliberately decoupled from the Hacker News wire format (HackerNewsItem)
/// so that upstream schema changes don't leak into the public contract.
/// </summary>
public sealed record StoryDto(
    string Title,
    string Uri,
    string PostedBy,
    DateTimeOffset Time,
    int Score,
    int CommentCount);
