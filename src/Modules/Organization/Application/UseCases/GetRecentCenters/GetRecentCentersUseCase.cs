using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;

namespace AriaHR.Modules.Organization.Application.UseCases.GetRecentCenters;

public class GetRecentCentersUseCase : IGetRecentCentersUseCase
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetRecentCentersUseCase(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
    }

    public async Task<PagedResult<RecentCenterDto>> ExecuteAsync(int page = 1, int pageSize = 5, CancellationToken cancellationToken = default)
    {
        return await _organizationRepository.GetRecentCentersPagedAsync(page, pageSize, cancellationToken);
    }
}
