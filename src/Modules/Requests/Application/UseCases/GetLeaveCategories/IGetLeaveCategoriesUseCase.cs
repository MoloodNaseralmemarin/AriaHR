using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.GetLeaveCategories;

public interface IGetLeaveCategoriesUseCase
{
    Task<IEnumerable<LeaveCategoryListItemDto>> ExecuteAsync(
        Guid organizationId,
        bool onlyActive = false,
        CancellationToken cancellationToken = default);
}
