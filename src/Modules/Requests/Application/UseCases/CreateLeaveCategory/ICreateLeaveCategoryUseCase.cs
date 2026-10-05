using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.CreateLeaveCategory;

public interface ICreateLeaveCategoryUseCase
{
    Task<LeaveCategoryDto> ExecuteAsync(
        CreateLeaveCategoryRequest request,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
