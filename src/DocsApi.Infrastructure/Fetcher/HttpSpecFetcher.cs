using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DocsApi.Domain.Entities;
using DocsApi.Domain.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DocsApi.Infrastructure.Fetcher;

public class HttpSpecFetcher : ISpecFetcher
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpSpecFetcher(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> FetchAsync(ServiceDefinition service, CancellationToken cancellationToken = default)
    {
        var clientName = service.Insecure ? "insecure" : "default";
        var client = _httpClientFactory.CreateClient(clientName);

        using var request = new HttpRequestMessage(HttpMethod.Get, service.SpecUrl);

        if (service.Auth is { Type: AuthType.Basic } auth)
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth.Username}:{auth.Password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        if (service.Headers is not null)
        {
            foreach (var (key, value) in service.Headers)
                request.Headers.TryAddWithoutValidation(key, value);
        }

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            throw new HttpRequestException($"Request to '{service.SpecUrl}' timed out.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var hasAuth = service.Auth is not null;
            throw new HttpRequestException(
                hasAuth
                    ? $"Credentials rejected by '{service.Id}' (HTTP 401). Check username/password in services.yml."
                    : $"'{service.Id}' requires authentication (HTTP 401). Configure 'auth' in services.yml.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to fetch spec for '{service.Id}': HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return ConvertToJson(content, service.Id);
    }

    private static string ConvertToJson(string content, string serviceId)
    {
        content = content.TrimStart();

        if (content.StartsWith('{') || content.StartsWith('['))
            return content;

        // Assume YAML — convert to JSON
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build();

            var obj = deserializer.Deserialize<object>(content);
            return JsonSerializer.Serialize(obj);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse spec for '{serviceId}' as YAML or JSON: {ex.Message}", ex);
        }
    }
}
