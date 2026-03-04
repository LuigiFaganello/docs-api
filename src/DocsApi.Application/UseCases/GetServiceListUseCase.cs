using DocsApi.Application.DTOs;
using DocsApi.Domain.Interfaces;

namespace DocsApi.Application.UseCases;

public class GetServiceListUseCase(IServiceRegistry registry)
{
    public IReadOnlyList<ServiceSummaryDto> Execute() =>
        registry.GetAll()
            .Select(s => new ServiceSummaryDto(s.Id, s.Name, s.Description))
            .ToList();
}
