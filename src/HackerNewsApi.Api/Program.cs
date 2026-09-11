using HackerNewsApi.Api.Middleware;
using HackerNewsApi.Application.Extensions;
using HackerNewsApi.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- IoC composition root -------------------------------------------------
// Each layer registers its own services via an extension method it owns.
// Program.cs never references a concrete Service/Client/Repository type
// directly — only the abstractions are used elsewhere in the app.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Hacker News Best Stories API",
        Version = "v1",
        Description = "Returns the best n Hacker News stories, ordered by score descending."
    });
});

builder.Services.AddApplicationServices();
builder.Services.AddHackerNewsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
