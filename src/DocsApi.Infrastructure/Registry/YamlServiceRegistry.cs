using DocsApi.Domain.Entities;
using DocsApi.Domain.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DocsApi.Infrastructure.Registry;

public class YamlServiceRegistry : IServiceRegistry
{
    private IReadOnlyList<ServiceDefinition> _services;

    public YamlServiceRegistry(string filePath)
    {
        _services = Load(filePath);
    }

    public IReadOnlyList<ServiceDefinition> GetAll() => _services;

    public ServiceDefinition? GetById(string id) =>
        _services.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<ServiceDefinition> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"services.yml not found at: {Path.GetFullPath(filePath)}");

        string yaml;
        try
        {
            yaml = File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read '{filePath}': {ex.Message}", ex);
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        ServicesConfig config;
        try
        {
            config = deserializer.Deserialize<ServicesConfig>(yaml)
                ?? throw new InvalidOperationException("services.yml is empty or invalid.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to parse '{filePath}': {ex.Message}", ex);
        }

        if (config.Services is null || config.Services.Count == 0)
            throw new InvalidOperationException("services.yml must contain at least one service.");

        var errors = new List<string>();

        foreach (var (svc, i) in config.Services.Select((s, i) => (s, i)))
        {
            if (string.IsNullOrWhiteSpace(svc.Id))
                errors.Add($"Service at index {i}: 'id' is required.");
            if (string.IsNullOrWhiteSpace(svc.SpecUrl))
                errors.Add($"Service '{svc.Id ?? $"[{i}]"}': 'specUrl' is required.");
        }

        var duplicates = config.Services
            .Where(s => !string.IsNullOrWhiteSpace(s.Id))
            .GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var dup in duplicates)
            errors.Add($"Duplicate service id: '{dup}'.");

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Invalid services.yml:\n{string.Join("\n", errors.Select(e => $"  - {e}"))}");

        return config.Services.Select(s => new ServiceDefinition
        {
            Id = s.Id!,
            Name = s.Name ?? s.Id!,
            SpecUrl = s.SpecUrl!,
            Description = s.Description,
            TtlSeconds = s.Ttl > 0 ? s.Ttl : 300,
            Insecure = s.Insecure,
            Auth = s.Auth is { Type: "basic" }
                ? new ServiceAuth(AuthType.Basic, s.Auth.Username ?? "", s.Auth.Password ?? "")
                : null,
            Headers = s.Headers is { Count: > 0 } ? s.Headers : null
        }).ToList();
    }

    // YAML deserialization models (internal)
    private class ServicesConfig
    {
        public List<ServiceEntry>? Services { get; set; }
    }

    private class ServiceEntry
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? SpecUrl { get; set; }
        public string? Description { get; set; }
        public AuthEntry? Auth { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public int Ttl { get; set; }
        public bool Insecure { get; set; }
    }

    private class AuthEntry
    {
        public string? Type { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
