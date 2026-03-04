using DocsApi.Domain.Interfaces;

namespace DocsApi.Application.UseCases;

public class RefreshServiceSpecUseCase(IServiceRegistry registry, ISpecCache cache)
{
    public void Execute(string serviceId)
    {
        var service = registry.GetById(serviceId)
            ?? throw new KeyNotFoundException($"Service '{serviceId}' not found.");

        cache.Remove(service.Id);
    }
}
