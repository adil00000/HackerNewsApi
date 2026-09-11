# Hacker News Best Stories API

An ASP.NET Core (.NET 8) Web API that returns the best *n* Hacker News stories,
ordered by score descending, sourced from the public Hacker News API.

```
GET /api/stories/best/{n}
```

Example: `GET /api/stories/best/10` returns the 10 highest-scored current best stories:

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  }
]
```

## How to run

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download), or Visual Studio 2022 17.8+.

**Visual Studio:** open `HackerNewsApi.sln`, set `HackerNewsApi.Api` as the startup project, and press F5.
Swagger UI opens automatically at `/swagger`.

**CLI:**
```bash
dotnet restore
dotnet run --project src/HackerNewsApi.Api
```

**Tests:**
```bash
dotnet test
```

## Architecture

The solution is split into four projects, dependencies flowing inward:

```
HackerNewsApi.Api            → ASP.NET Core host: controllers, DI wiring, middleware, Swagger
HackerNewsApi.Application     → Use cases and public contracts: IStoryService, StoryDto,
                                 IHackerNewsClient (the boundary Infrastructure implements)
HackerNewsApi.Infrastructure  → HTTP client for the real Hacker News API, caching,
                                 retry/circuit-breaker policies, throttling
HackerNewsApi.Tests           → Unit tests for the Application layer
```

`Api` and `Infrastructure` both depend on `Application`; `Application` depends on nothing else
in the solution. This is Dependency Inversion in practice — the business rule
("fetch, filter, sort, take n") never references HttpClient, IMemoryCache, or Polly.

### Design patterns used

- **Dependency Injection / IoC container** — every class receives its
  dependencies through its constructor and is registered in the built-in
  ASP.NET Core container (see the `AddApplicationServices` and
  `AddHackerNewsInfrastructure` extension methods, called once from `Program.cs`).
- **Decorator** — `CachingHackerNewsClient` wraps `HackerNewsApiClient`, both
  implementing `IHackerNewsClient`. Caching was added without changing the HTTP
  client or the service that consumes it.
- **Strategy (via Polly policies)** — retry (exponential backoff) and circuit
  breaker are configured declaratively on the `HttpClient` registration and can
  be swapped or extended independently of the calling code.
- **Options pattern** — all tunables (cache durations, retry count, concurrency
  limit) are bound from `appsettings.json` into a strongly-typed `HackerNewsOptions`.
- **Middleware pipeline** — a single `ExceptionHandlingMiddleware` centralises
  error-to-HTTP-response translation instead of try/catch in every controller.

### SOLID

- **Single Responsibility** — `StoryService` only orchestrates the use case;
  `HackerNewsApiClient` only knows HTTP; `CachingHackerNewsClient` only knows
  caching; `MemoryCacheService` only knows the cache store; the controller only
  translates HTTP ⇄ calls.
- **Open/Closed** — caching, retry, and circuit-breaking were all added as new
  classes/registrations around existing code, not by editing it. A future
  Redis-backed cache would mean writing a new `ICacheService` implementation,
  not touching `StoryService` or the controller.
- **Liskov Substitution** — any `IHackerNewsClient` (raw, cached, or a future
  fake for tests) is interchangeable wherever the interface is used.
- **Interface Segregation** — `IStoryService`, `IHackerNewsClient`, and
  `ICacheService` are each narrow and specific to one consumer's needs, rather
  than one large "IHackerNewsEverything" interface.
- **Dependency Inversion** — the Application layer defines `IHackerNewsClient`;
  Infrastructure implements it. Application never references Infrastructure.

## Avoiding overload of the upstream Hacker News API

Three complementary mechanisms:

1. **Caching** (`CachingHackerNewsClient` + `MemoryCacheService`) — the best-story
   id list and individual story details are cached in memory (default: 2 and 5
   minutes respectively, configurable). Per-key locking in `MemoryCacheService`
   collapses concurrent cache misses for the same key into a single upstream
   call rather than a stampede.
2. **Bounded concurrency** — `HackerNewsApiClient` uses a `SemaphoreSlim` to cap
   how many requests are in flight to Hacker News at once (default: 10),
   regardless of how many concurrent callers hit our own API.
3. **Resilience policies** — Polly retry (exponential backoff, default 3
   attempts) and a circuit breaker (opens after 5 consecutive failures for 30s)
   protect against retry storms if Hacker News is degraded.

## Assumptions

- `beststories.json` is **not** assumed to already be sorted by score — the API
  fetches details for every id it returns and sorts locally, since that is the
  only way to guarantee correctness of "best n by score".
- Items that are `deleted`, `dead`, or have no title are treated as unpublishable
  and excluded from results, since they don't have the fields the response
  contract requires.
- `n` is validated as a positive integer; a non-positive or missing value
  returns `400 Bad Request`. `n` larger than the number of stories Hacker News
  currently reports simply returns as many as are available.
- Short-lived in-memory caching (rather than always-live data) is an acceptable
  trade-off for this exercise given the "avoid overloading the upstream API"
  requirement; scores a few minutes stale is treated as fine.
- Single-instance in-memory caching is sufficient for the scope of this test
  (see enhancements below for multi-instance deployment).

## Enhancements given more time

- Swap `IMemoryCache` for a distributed cache (e.g. Redis) via a new
  `ICacheService` implementation, so caching remains correct if the API is
  scaled out to multiple instances.
- Add a background refresh (e.g. `IHostedService`) that periodically warms the
  best-story-ids cache proactively, so user-facing requests are never the ones
  paying the cost of a cold cache.
- Add response caching / ETags on the controller action itself for HTTP-level
  caching by clients and intermediaries.
- Add integration tests using `WebApplicationFactory` with a stubbed
  `IHackerNewsClient`, and a contract/HTTP test against a mocked Hacker News
  endpoint (e.g. via `HttpMessageHandler` fakes) to verify the Polly policies.
- Add structured logging/telemetry (e.g. OpenTelemetry) around upstream call
  latency and cache hit rate to make the throttling/caching tunables
  data-driven rather than guessed defaults.
- Support pagination or a maximum-`n` cap to protect against a caller
  requesting an unreasonably large number of stories.
- Add API versioning and rate limiting (`Microsoft.AspNetCore.RateLimiting`) at
  the edge of our own API, not just for the upstream Hacker News calls.
