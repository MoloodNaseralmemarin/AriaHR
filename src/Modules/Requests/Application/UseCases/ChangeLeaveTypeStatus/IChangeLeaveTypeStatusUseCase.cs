using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.ChangeLeaveTypeStatus;

public interface IChangeLeaveTypeStatusUseCase
{
    Task<LeaveTypeResponse> ExecuteAsync(Guid id, ChangeLeaveTypeStatusRequest request, Guid organizationId, Guid currentUserId, CancellationToken cancellationToken = default);
}
