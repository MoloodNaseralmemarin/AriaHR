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
            ManagerFullName = GetManagerFullName(org.ManagerFirstName, org.ManagerLastName),
            ManagerMobile = NormalizePhoneNumber(org.ManagerMobile),
            CenterPhone = NormalizePhoneNumber(org.Phone),
            IsActive = org.IsActive,
            CreatedAtUtc = org.CreatedAtUtc
        });
    }

    private static string? NormalizePhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        return phone.Trim();
    }

    private static string? GetManagerFullName(string? firstName, string? lastName)
    {
        var trimmedFirstName = firstName?.Trim();
        var trimmedLastName = lastName?.Trim();

        var fullName = $"{trimmedFirstName} {trimmedLastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }
}
