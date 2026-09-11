using FluentAssertions;
using HackerNewsApi.Application.DTOs;
using HackerNewsApi.Application.Interfaces;
using HackerNewsApi.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HackerNewsApi.Tests;

/// <summary>
/// Because StoryService depends only on the IHackerNewsClient abstraction
/// (constructor injection, Dependency Inversion), it can be tested completely
/// in isolation with a mock — no HTTP, no cache, no real Hacker News API.
/// </summary>
public class StoryServiceTests
{
    private static HackerNewsItem MakeItem(int id, int score, string title = "title", bool deleted = false, bool dead = false) =>
        new()
        {
            Id = id,
            Title = title,
            Score = score,
            By = "author",
            Time = 1_600_000_000,
            Descendants = 0,
            Deleted = deleted,
            Dead = dead
        };

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsStories_OrderedByScoreDescending()
    {
        var mockClient = new Mock<IHackerNewsClient>();
        mockClient.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1, 2, 3 });
        mockClient.Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeItem(1, score: 50));
        mockClient.Setup(c => c.GetItemAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(MakeItem(2, score: 200));
        mockClient.Setup(c => c.GetItemAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(MakeItem(3, score: 120));

        var sut = new StoryService(mockClient.Object, NullLogger<StoryService>.Instance);

        var result = await sut.GetBestStoriesAsync(3, CancellationToken.None);

        result.Select(s => s.Score).Should().ContainInOrder(200, 120, 50);
    }

    [Fact]
    public async Task GetBestStoriesAsync_LimitsResultsToRequestedCount()
    {
        var mockClient = new Mock<IHackerNewsClient>();
        mockClient.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1, 2, 3, 4 });
        for (var i = 1; i <= 4; i++)
        {
            var id = i;
            mockClient.Setup(c => c.GetItemAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(MakeItem(id, score: id * 10));
        }

        var sut = new StoryService(mockClient.Object, NullLogger<StoryService>.Instance);

        var result = await sut.GetBestStoriesAsync(2, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(s => s.Score).Should().ContainInOrder(40, 30);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ExcludesDeletedDeadAndTitlelessItems()
    {
        var mockClient = new Mock<IHackerNewsClient>();
        mockClient.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1, 2, 3, 4 });
        mockClient.Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeItem(1, 100, deleted: true));
        mockClient.Setup(c => c.GetItemAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(MakeItem(2, 90, dead: true));
        mockClient.Setup(c => c.GetItemAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(MakeItem(3, 80, title: ""));
        mockClient.Setup(c => c.GetItemAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(MakeItem(4, 70));

        var sut = new StoryService(mockClient.Object, NullLogger<StoryService>.Instance);

        var result = await sut.GetBestStoriesAsync(10, CancellationToken.None);

        result.Should().ContainSingle().Which.Score.Should().Be(70);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsEmpty_WhenNoIdsAvailable()
    {
        var mockClient = new Mock<IHackerNewsClient>();
        mockClient.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<int>());

        var sut = new StoryService(mockClient.Object, NullLogger<StoryService>.Instance);

        var result = await sut.GetBestStoriesAsync(5, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetBestStoriesAsync_Throws_WhenCountIsNotPositive(int invalidCount)
    {
        var mockClient = new Mock<IHackerNewsClient>();
        var sut = new StoryService(mockClient.Object, NullLogger<StoryService>.Instance);

        var act = async () => await sut.GetBestStoriesAsync(invalidCount, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
