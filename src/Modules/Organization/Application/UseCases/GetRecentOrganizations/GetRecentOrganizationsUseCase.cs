using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;

namespace AriaHR.Modules.Organization.Application.UseCases.GetRecentOrganizations;

public class GetRecentOrganizationsUseCase : IGetRecentOrganizationsUseCase
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetRecentOrganizationsUseCase(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
    }

    public async Task<IReadOnlyList<RecentOrganizationDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var orgs = await _organizationRepository.GetRecentOrganizationsAsync(3, cancellationToken);

        return orgs.Select(x => new RecentOrganizationDto
        {
            Id = x.Id,
            Name = x.Name,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();
    }
}
