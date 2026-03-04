using DocsApi.Application.UseCases;
using DocsApi.Domain.Entities;
using DocsApi.Domain.Interfaces;
using Moq;

namespace DocsApi.Unit;

public class GetServiceSpecUseCaseTests
{
    private static ServiceDefinition MakeService(string id = "svc") => new()
    {
        Id = id,
        Name = "Test Service",
        SpecUrl = "http://test/spec.json",
        TtlSeconds = 300
    };

    [Fact]
    public async Task Execute_ReturnsCacheHit_WhenSpecIsFresh()
    {
        var registry = new Mock<IServiceRegistry>();
        var fetcher = new Mock<ISpecFetcher>();
        var cache = new Mock<ISpecCache>();

        var service = MakeService();
        var cached = new CachedSpec { RawJson = "{\"cached\":true}", CachedAt = DateTime.UtcNow };

        registry.Setup(r => r.GetById("svc")).Returns(service);
        cache.Setup(c => c.TryGet("svc")).Returns(cached);

        var useCase = new GetServiceSpecUseCase(registry.Object, fetcher.Object, cache.Object);
        var result = await useCase.ExecuteAsync("svc");

        Assert.Equal(SpecCacheStatus.Hit, result.CacheStatus);
        Assert.Equal("{\"cached\":true}", result.Json);
        fetcher.Verify(f => f.FetchAsync(It.IsAny<ServiceDefinition>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_FetchesAndCaches_WhenCacheMiss()
    {
        var registry = new Mock<IServiceRegistry>();
        var fetcher = new Mock<ISpecFetcher>();
        var cache = new Mock<ISpecCache>();

        var service = MakeService();
        registry.Setup(r => r.GetById("svc")).Returns(service);
        cache.Setup(c => c.TryGet("svc")).Returns((CachedSpec?)null);
        fetcher.Setup(f => f.FetchAsync(service, It.IsAny<CancellationToken>())).ReturnsAsync("{\"fresh\":true}");

        var useCase = new GetServiceSpecUseCase(registry.Object, fetcher.Object, cache.Object);
        var result = await useCase.ExecuteAsync("svc");

        Assert.Equal(SpecCacheStatus.Miss, result.CacheStatus);
        Assert.Equal("{\"fresh\":true}", result.Json);
        cache.Verify(c => c.Set("svc", It.IsAny<CachedSpec>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ReturnsStale_WhenFetchFailsButCacheExists()
    {
        var registry = new Mock<IServiceRegistry>();
        var fetcher = new Mock<ISpecFetcher>();
        var cache = new Mock<ISpecCache>();

        var service = MakeService();
        var stale = new CachedSpec { RawJson = "{\"stale\":true}", CachedAt = DateTime.UtcNow.AddSeconds(-999) };

        registry.Setup(r => r.GetById("svc")).Returns(service);
        cache.Setup(c => c.TryGet("svc")).Returns(stale);
        fetcher.Setup(f => f.FetchAsync(service, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var useCase = new GetServiceSpecUseCase(registry.Object, fetcher.Object, cache.Object);
        var result = await useCase.ExecuteAsync("svc");

        Assert.Equal(SpecCacheStatus.Stale, result.CacheStatus);
        Assert.Equal("{\"stale\":true}", result.Json);
    }

    [Fact]
    public async Task Execute_ThrowsKeyNotFound_WhenServiceUnknown()
    {
        var registry = new Mock<IServiceRegistry>();
        var fetcher = new Mock<ISpecFetcher>();
        var cache = new Mock<ISpecCache>();

        registry.Setup(r => r.GetById("unknown")).Returns((ServiceDefinition?)null);

        var useCase = new GetServiceSpecUseCase(registry.Object, fetcher.Object, cache.Object);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.ExecuteAsync("unknown"));
    }
}
