namespace DocsApi.Domain.Entities;

public class CachedSpec
{
    public required string RawJson { get; init; }
    public DateTime CachedAt { get; init; } = DateTime.UtcNow;

    public bool IsStale(int ttlSeconds) =>
        (DateTime.UtcNow - CachedAt).TotalSeconds >= ttlSeconds;
}
