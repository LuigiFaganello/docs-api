namespace DocsApi.Domain.Entities;

public class ServiceDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string SpecUrl { get; init; }
    public string? Description { get; init; }
    public ServiceAuth? Auth { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
    public int TtlSeconds { get; init; } = 300;
    public bool Insecure { get; init; } = false;
}
