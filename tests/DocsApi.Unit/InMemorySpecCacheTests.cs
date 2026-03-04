using DocsApi.Domain.Entities;
using DocsApi.Infrastructure.Cache;

namespace DocsApi.Unit;

public class InMemorySpecCacheTests
{
    [Fact]
    public void TryGet_ReturnsNull_WhenNotSet()
    {
        var cache = new InMemorySpecCache();
        Assert.Null(cache.TryGet("svc-a"));
    }

    [Fact]
    public void Set_ThenTryGet_ReturnsCachedSpec()
    {
        var cache = new InMemorySpecCache();
        var spec = new CachedSpec { RawJson = "{}", CachedAt = DateTime.UtcNow };

        cache.Set("svc-a", spec);

        var result = cache.TryGet("svc-a");
        Assert.NotNull(result);
        Assert.Equal("{}", result.RawJson);
    }

    [Fact]
    public void Remove_ClearsSpecForId()
    {
        var cache = new InMemorySpecCache();
        cache.Set("svc-a", new CachedSpec { RawJson = "{}", CachedAt = DateTime.UtcNow });

        cache.Remove("svc-a");

        Assert.Null(cache.TryGet("svc-a"));
    }

    [Fact]
    public void Clear_RemovesAllSpecs()
    {
        var cache = new InMemorySpecCache();
        cache.Set("svc-a", new CachedSpec { RawJson = "{}", CachedAt = DateTime.UtcNow });
        cache.Set("svc-b", new CachedSpec { RawJson = "{}", CachedAt = DateTime.UtcNow });

        cache.Clear();

        Assert.Null(cache.TryGet("svc-a"));
        Assert.Null(cache.TryGet("svc-b"));
    }

    [Fact]
    public void IsStale_ReturnsFalse_WhenWithinTtl()
    {
        var spec = new CachedSpec { RawJson = "{}", CachedAt = DateTime.UtcNow };
        Assert.False(spec.IsStale(300));
    }

    [Fact]
    public void IsStale_ReturnsTrue_WhenTtlExceeded()
    {
        var spec = new CachedSpec { RawJson = "{}", CachedAt = DateTime.UtcNow.AddSeconds(-400) };
        Assert.True(spec.IsStale(300));
    }
}
