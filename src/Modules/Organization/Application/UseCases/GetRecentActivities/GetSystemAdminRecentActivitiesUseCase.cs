using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;

namespace AriaHR.Modules.Organization.Application.UseCases.GetRecentActivities;

public class GetSystemAdminRecentActivitiesUseCase : IGetSystemAdminRecentActivitiesUseCase
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetSystemAdminRecentActivitiesUseCase(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
    }

    public async Task<PagedResult<RecentActivityDto>> ExecuteAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return await _organizationRepository.GetRecentActivitiesPagedAsync(page, pageSize, cancellationToken);
    }
}
