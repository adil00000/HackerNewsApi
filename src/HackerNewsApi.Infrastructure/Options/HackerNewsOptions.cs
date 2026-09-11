namespace HackerNewsApi.Infrastructure.Options;

/// <summary>
/// Bound from the "HackerNews" section of appsettings.json.
/// Centralising the tunables here means the throttling/caching behaviour can
/// be changed per-environment without touching code (Open/Closed in practice).
/// </summary>
public sealed class HackerNewsOptions
{
    public const string SectionName = "HackerNews";

    /// <summary>Base address of the Hacker News Firebase API.</summary>
    public string BaseUrl { get; set; } = "https://hacker-news.firebaseio.com/v0/";

    /// <summary>Maximum number of concurrent outbound requests to the upstream API.</summary>
    public int MaxConcurrentUpstreamRequests { get; set; } = 10;

    /// <summary>How long the list of best-story ids is cached for.</summary>
    public TimeSpan BestStoryIdsCacheDuration { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>How long an individual story's details are cached for.</summary>
    public TimeSpan StoryItemCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Number of retry attempts on transient HTTP failures.</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>Timeout applied to each individual upstream HTTP call.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
