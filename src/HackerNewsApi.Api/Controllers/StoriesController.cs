using HackerNewsApi.Application.DTOs;
using HackerNewsApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsApi.Api.Controllers;

/// <summary>
/// Thin HTTP boundary: this controller has one job — translate an HTTP request
/// into a call on IStoryService and shape the response. All business logic
/// (Dependency Inversion: the controller depends on the IStoryService
/// abstraction, injected by the IoC container, not on a concrete class).
/// </summary>
[ApiController]
[Route("api/stories")]
[Produces("application/json")]
public sealed class StoriesController : ControllerBase
{
    private readonly IStoryService _storyService;
    private readonly ILogger<StoriesController> _logger;

    public StoriesController(IStoryService storyService, ILogger<StoriesController> logger)
    {
        _storyService = storyService ?? throw new ArgumentNullException(nameof(storyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns the best <paramref name="n"/> Hacker News stories, ordered by score descending.
    /// </summary>
    /// <param name="n">Number of stories to return. Must be a positive integer.</param>
    [HttpGet("best/{n:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<StoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<StoryDto>>> GetBestStories(int n, CancellationToken cancellationToken)
    {
        if (n <= 0)
        {
            return ValidationProblem($"'{nameof(n)}' must be a positive integer.");
        }

        _logger.LogInformation("Retrieving best {Count} stories.", n);
        var stories = await _storyService.GetBestStoriesAsync(n, cancellationToken);
        return Ok(stories);
    }
}
