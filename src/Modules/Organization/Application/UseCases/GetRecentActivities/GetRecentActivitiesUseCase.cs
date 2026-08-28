using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;

namespace AriaHR.Modules.Organization.Application.UseCases.GetRecentActivities;

public class GetRecentActivitiesUseCase : IGetRecentActivitiesUseCase
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetRecentActivitiesUseCase(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
    }

    public async Task<IReadOnlyList<RecentActivityDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await _organizationRepository.GetRecentActivitiesAsync(3, cancellationToken);
    }
}
