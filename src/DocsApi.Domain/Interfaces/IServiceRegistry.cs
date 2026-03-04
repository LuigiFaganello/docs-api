using DocsApi.Domain.Entities;

namespace DocsApi.Domain.Interfaces;

public interface IServiceRegistry
{
    IReadOnlyList<ServiceDefinition> GetAll();
    ServiceDefinition? GetById(string id);
}
