using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.ActivateLeaveCategory;

public interface IActivateLeaveCategoryUseCase
{
    Task<LeaveCategoryDto> ExecuteAsync(
        Guid id,
        Guid userId,
        Guid userOrganizationId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default);
}
