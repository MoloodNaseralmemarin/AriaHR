using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetOrganizations;

public interface IGetOrganizationsUseCase
{
    Task<IEnumerable<OrganizationListItemDto>> ExecuteAsync(CancellationToken cancellationToken = default);
}
