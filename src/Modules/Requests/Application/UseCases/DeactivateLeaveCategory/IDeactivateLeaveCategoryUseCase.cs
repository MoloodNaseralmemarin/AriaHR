using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.DeactivateLeaveCategory;

public interface IDeactivateLeaveCategoryUseCase
{
    Task<LeaveCategoryDto> ExecuteAsync(
        Guid id,
        Guid userId,
        Guid userOrganizationId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default);
}
