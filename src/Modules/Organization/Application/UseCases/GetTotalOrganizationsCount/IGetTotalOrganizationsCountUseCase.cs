using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetTotalOrganizationsCount;

public interface IGetTotalOrganizationsCountUseCase
{
    Task<OrganizationCountResponse> ExecuteAsync(CancellationToken cancellationToken = default);
}
