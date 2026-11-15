using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetWorkLocations;

public interface IGetWorkLocationsUseCase
{
    Task<WorkLocationDto> GetByIdAsync(
        Guid workLocationId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkLocationDto>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
