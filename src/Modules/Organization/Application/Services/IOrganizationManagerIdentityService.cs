using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.Services;

public interface IOrganizationManagerIdentityService
{
    Task<OrganizationDto> CreateOrganizationWithManagerAsync(
        CreateOrganizationRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
}
