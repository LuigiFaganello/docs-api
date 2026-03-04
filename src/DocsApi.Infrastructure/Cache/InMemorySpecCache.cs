using System.Collections.Concurrent;
using DocsApi.Domain.Entities;
using DocsApi.Domain.Interfaces;

namespace DocsApi.Infrastructure.Cache;

public class InMemorySpecCache : ISpecCache
{
    private readonly ConcurrentDictionary<string, CachedSpec> _cache = new(StringComparer.OrdinalIgnoreCase);

    public CachedSpec? TryGet(string id) =>
        _cache.TryGetValue(id, out var spec) ? spec : null;

    public void Set(string id, CachedSpec spec) =>
        _cache[id] = spec;

    public void Remove(string id) =>
        _cache.TryRemove(id, out _);

    public void Clear() =>
        _cache.Clear();
}
