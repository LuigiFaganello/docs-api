using System.Net;
using System.Text.Json;
using DocsApi.Domain.Interfaces;
using DocsApi.Infrastructure.Registry;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DocsApi.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _tempFile;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _tempFile = Path.GetTempFileName();
        File.WriteAllText(_tempFile, """
            services:
              - id: test-svc
                name: Test Service
                specUrl: http://localhost/nonexistent/spec.json
                ttl: 300
            """);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IServiceRegistry>(_ => new YamlServiceRegistry(_tempFile));
            });
        });
    }

    [Fact]
    public async Task GetHealth_Returns200()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ok", body);
    }

    [Fact]
    public async Task Root_RedirectsToDocs()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/docs", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GetServices_ReturnsRegisteredServices()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/services");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var docs = JsonDocument.Parse(json).RootElement;

        Assert.Equal(JsonValueKind.Array, docs.ValueKind);
        Assert.Equal(1, docs.GetArrayLength());
        Assert.Equal("test-svc", docs[0].GetProperty("id").GetString());
        Assert.Equal("Test Service", docs[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetSpec_UnknownService_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/services/nonexistent/spec");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSpec_KnownServiceUnavailable_Returns502()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/services/test-svc/spec");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
