using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.UpdateCenterStatus;

public interface IUpdateCenterStatusUseCase
{
    Task<BranchListItemDto> ExecuteAsync(
        Guid centerId,
        CenterStatusUpdateRequestDto request,
        Guid currentUserId,
        bool isSystemAdmin,
        Guid? userOrganizationId,
        CancellationToken cancellationToken = default);
}
