using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.RejectLeaveRequest;

public interface IRejectLeaveRequestUseCase
{
    Task<LeaveRequestDto> ExecuteAsync(
        Guid id,
        RejectLeaveRequestRequest request,
        Guid userId,
        Guid userOrganizationId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default);
}
