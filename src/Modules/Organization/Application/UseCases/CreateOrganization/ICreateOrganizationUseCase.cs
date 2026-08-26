using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;

public interface ICreateOrganizationUseCase
{
    Task<OrganizationDto> ExecuteAsync(
        CreateOrganizationRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
}
