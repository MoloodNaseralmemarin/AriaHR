using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.UpdateLeaveCategory;

public interface IUpdateLeaveCategoryUseCase
{
    Task<LeaveCategoryDto> ExecuteAsync(
        Guid id,
        UpdateLeaveCategoryRequest request,
        Guid userId,
        Guid userOrganizationId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default);
}
