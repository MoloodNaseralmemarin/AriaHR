using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.GetLeaveCategories;

public interface IGetLeaveCategoriesUseCase
{
    Task<IEnumerable<LeaveCategoryDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
