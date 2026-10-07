using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.GetLeaveCategoryById;

public interface IGetLeaveCategoryByIdUseCase
{
    Task<LeaveCategoryDto?> ExecuteAsync(
        Guid id,
        Guid userOrganizationId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default);
}
