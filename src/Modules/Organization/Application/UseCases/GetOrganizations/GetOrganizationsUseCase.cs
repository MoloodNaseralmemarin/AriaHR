using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;

namespace AriaHR.Modules.Organization.Application.UseCases.GetOrganizations;

public class GetOrganizationsUseCase : IGetOrganizationsUseCase
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetOrganizationsUseCase(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
    }

    public async Task<IEnumerable<OrganizationListItemDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var organizations = await _organizationRepository.GetOrganizationsAsync(cancellationToken);

        return organizations.Select(org => new OrganizationListItemDto
        {
            Id = org.Id,
            Name = org.Name,
            Code = org.Code,
            OrganizationType = org.OrganizationType,
            IsActive = org.IsActive,
            CreatedAtUtc = org.CreatedAtUtc
        });
    }
}
