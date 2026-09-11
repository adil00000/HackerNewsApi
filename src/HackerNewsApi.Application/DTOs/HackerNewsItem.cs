namespace HackerNewsApi.Application.DTOs;

/// <summary>
/// Mirrors the subset of fields we need from the Hacker News "item" endpoint:
/// https://hacker-news.firebaseio.com/v0/item/{id}.json
/// </summary>
public sealed class HackerNewsItem
{
    public int Id { get; init; }
    public string? Type { get; init; }
    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? By { get; init; }
    public long Time { get; init; }
    public int Score { get; init; }
    public int Descendants { get; init; }
    public bool Dead { get; init; }
    public bool Deleted { get; init; }
}
