using HackerNewsApi.Application.Interfaces;
using HackerNewsApi.Infrastructure.Caching;
using HackerNewsApi.Infrastructure.HackerNews;
using HackerNewsApi.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace HackerNewsApi.Infrastructure.Extensions;

/// <summary>
/// Composition root helper for the Infrastructure layer. This is the one place
/// that knows the concrete implementation types and how they're decorated —
/// everywhere else in the codebase talks only to IHackerNewsClient.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHackerNewsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HackerNewsOptions>()
            .Bind(configuration.GetSection(HackerNewsOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Register the concrete HTTP client, resilient via Polly retry + circuit breaker.
        services.AddHttpClient<HackerNewsApiClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HackerNewsOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.RequestTimeout;
            })
            .AddPolicyHandler((sp, _) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HackerNewsOptions>>().Value;
                return GetRetryPolicy(options.RetryCount);
            })
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // IHackerNewsClient resolves to the caching decorator wrapping the real
        // HTTP client (Decorator pattern), so every consumer automatically gets
        // caching without knowing it exists.
        services.AddScoped<IHackerNewsClient>(sp =>
        {
            var innerClient = sp.GetRequiredService<HackerNewsApiClient>();
            var cache = sp.GetRequiredService<ICacheService>();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HackerNewsOptions>>();
            return new CachingHackerNewsClient(innerClient, cache, options);
        });

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount) =>
        HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx and 408
            .WaitAndRetryAsync(
                retryCount,
                attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))); // exponential backoff

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
}
