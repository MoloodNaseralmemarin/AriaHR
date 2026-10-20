using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.UpdateCenter;

public interface IUpdateCenterUseCase
{
    Task<BranchListItemDto> ExecuteAsync(
        Guid centerId,
        UpdateBranchRequestDto request,
        Guid currentUserId,
        bool isSystemAdmin,
        Guid? userOrganizationId,
        CancellationToken cancellationToken = default);
}
