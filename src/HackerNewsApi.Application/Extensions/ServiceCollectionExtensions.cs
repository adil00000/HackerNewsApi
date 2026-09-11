using HackerNewsApi.Application.Interfaces;
using HackerNewsApi.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HackerNewsApi.Application.Extensions;

/// <summary>
/// Composition root helper: wires this layer's own services into the IoC container.
/// Keeping registration next to the code it registers (rather than one giant
/// Program.cs) is what makes the Api project's Program.cs stay thin and Open/Closed
/// with respect to new use cases.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IStoryService, StoryService>();
        return services;
    }
}
