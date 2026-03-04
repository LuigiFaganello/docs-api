using DocsApi.Domain.Entities;

namespace DocsApi.Domain.Interfaces;

public interface ISpecCache
{
    CachedSpec? TryGet(string id);
    void Set(string id, CachedSpec spec);
    void Remove(string id);
    void Clear();
}
