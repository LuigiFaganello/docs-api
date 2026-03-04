using DocsApi.Domain.Entities;
using DocsApi.Domain.Interfaces;

namespace DocsApi.Application.UseCases;

public enum SpecCacheStatus { Hit, Miss, Stale }

public record SpecResult(string Json, SpecCacheStatus CacheStatus);

public class GetServiceSpecUseCase(
    IServiceRegistry registry,
    ISpecFetcher fetcher,
    ISpecCache cache)
{
    public async Task<SpecResult> ExecuteAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        var service = registry.GetById(serviceId)
            ?? throw new KeyNotFoundException($"Service '{serviceId}' not found.");

        var cached = cache.TryGet(serviceId);

        if (cached is not null && !cached.IsStale(service.TtlSeconds))
            return new SpecResult(cached.RawJson, SpecCacheStatus.Hit);

        try
        {
            var json = await fetcher.FetchAsync(service, cancellationToken);
            cache.Set(serviceId, new CachedSpec { RawJson = json, CachedAt = DateTime.UtcNow });
            return new SpecResult(json, SpecCacheStatus.Miss);
        }
        catch
        {
            if (cached is not null)
                return new SpecResult(cached.RawJson, SpecCacheStatus.Stale);

            throw;
        }
    }
}
