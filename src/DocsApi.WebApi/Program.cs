using System.Text.Json;
using DocsApi.Application.UseCases;
using DocsApi.Domain.Interfaces;
using DocsApi.Infrastructure.Cache;
using DocsApi.Infrastructure.Fetcher;
using DocsApi.Infrastructure.Registry;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var servicesFile = Environment.GetEnvironmentVariable("SERVICES_FILE")
    ?? builder.Configuration["ServicesFile"]
    ?? "services.yml";

var fetchTimeout = int.TryParse(builder.Configuration["FetchTimeoutSeconds"], out var t) ? t : 10;
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// --- HTTP clients ---
builder.Services.AddHttpClient("default")
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(fetchTimeout));

builder.Services.AddHttpClient("insecure")
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(fetchTimeout))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

// --- Domain / Infrastructure ---
builder.Services.AddSingleton<IServiceRegistry>(_ => new YamlServiceRegistry(servicesFile));
builder.Services.AddSingleton<ISpecCache, InMemorySpecCache>();
builder.Services.AddSingleton<ISpecFetcher, HttpSpecFetcher>();

// --- Use Cases ---
builder.Services.AddScoped<GetServiceListUseCase>();
builder.Services.AddScoped<GetServiceSpecUseCase>();
builder.Services.AddScoped<RefreshServiceSpecUseCase>();
builder.Services.AddScoped<ClearCacheUseCase>();

// --- ASP.NET Core ---
builder.Services.AddControllers();

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
}

var app = builder.Build();

if (allowedOrigins.Length > 0)
    app.UseCors();

// Scalar multi-document viewer: dynamically renders all registered services
app.MapGet("/docs", (IServiceRegistry reg) =>
{
    var services = reg.GetAll().ToList();

    if (services.Count == 0)
        return Results.Content("""
            <!DOCTYPE html>
            <html><head><title>docs-api</title><meta charset="utf-8"/></head>
            <body style="font-family:sans-serif;padding:2rem">
              <p>No services configured. Add entries to <code>services.yml</code>.</p>
            </body></html>
            """, "text/html");

    var sources = JsonSerializer.Serialize(
        services.Select(s => new { url = $"/api/services/{s.Id}/spec", title = s.Name, slug = s.Id }),
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    var html = $$"""
        <!DOCTYPE html>
        <html>
        <head>
          <title>docs-api — Centralização de Documentações</title>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <style>* { margin: 0; box-sizing: border-box; } body { height: 100vh; }</style>
        </head>
        <body>
          <div id="app"></div>
          <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
          <script>
            Scalar.createApiReference('#app', {
              sources: {{sources}}
            })
          </script>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html");
})
.ExcludeFromDescription();

app.MapGet("/", (IServiceRegistry reg) =>
{
    return reg.GetAll().Any()
        ? Results.Redirect("/docs")
        : Results.Ok(new { message = "No services configured. Add entries to services.yml." });
})
.ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .ExcludeFromDescription();

app.MapControllers();

app.Run();

// Expose for integration tests
public partial class Program { }
