using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.UpdateWorkLocation;

public interface IUpdateWorkLocationUseCase
{
    Task<WorkLocationDto> ExecuteAsync(
        Guid workLocationId,
        UpdateWorkLocationRequest request,
        Guid currentUserId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default);
}
