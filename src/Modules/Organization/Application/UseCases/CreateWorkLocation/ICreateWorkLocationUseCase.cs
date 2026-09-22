using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;

public interface ICreateWorkLocationUseCase
{
    Task<WorkLocationDto> ExecuteAsync(
        CreateWorkLocationRequest request,
        Guid organizationId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
}
