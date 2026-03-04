using DocsApi.Domain.Entities;

namespace DocsApi.Domain.Interfaces;

public interface ISpecFetcher
{
    Task<string> FetchAsync(ServiceDefinition service, CancellationToken cancellationToken = default);
}
