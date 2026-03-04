using DocsApi.Domain.Interfaces;

namespace DocsApi.Application.UseCases;

public class ClearCacheUseCase(ISpecCache cache)
{
    public void Execute() => cache.Clear();
}
