using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Services;

namespace AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;

public class CreateOrganizationUseCase : ICreateOrganizationUseCase
{
    private readonly IOrganizationManagerIdentityService _managerIdentityService;

    public CreateOrganizationUseCase(IOrganizationManagerIdentityService managerIdentityService)
    {
        _managerIdentityService = managerIdentityService ?? throw new ArgumentNullException(nameof(managerIdentityService));
    }

    public Task<OrganizationDto> ExecuteAsync(
        CreateOrganizationRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return _managerIdentityService.CreateOrganizationWithManagerAsync(request, createdByUserId, cancellationToken);
    }
}
