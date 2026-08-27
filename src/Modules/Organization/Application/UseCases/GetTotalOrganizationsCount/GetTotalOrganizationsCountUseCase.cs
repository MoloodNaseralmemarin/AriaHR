using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;

namespace AriaHR.Modules.Organization.Application.UseCases.GetTotalOrganizationsCount;

public class GetTotalOrganizationsCountUseCase : IGetTotalOrganizationsCountUseCase
{
    private readonly IOrganizationRepository _repository;

    public GetTotalOrganizationsCountUseCase(IOrganizationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<OrganizationCountResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var count = await _repository.CountAsync(cancellationToken);
        return new OrganizationCountResponse
        {
            TotalCount = count
        };
    }
}
