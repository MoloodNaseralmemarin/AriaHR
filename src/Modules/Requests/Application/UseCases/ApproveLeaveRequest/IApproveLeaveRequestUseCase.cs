using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.ApproveLeaveRequest;

public interface IApproveLeaveRequestUseCase
{
    Task<LeaveRequestDto> ExecuteAsync(
        Guid id,
        Guid userId,
        Guid userOrganizationId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default);
}
