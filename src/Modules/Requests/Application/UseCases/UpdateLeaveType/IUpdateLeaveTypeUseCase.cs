using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.UpdateLeaveType;

public interface IUpdateLeaveTypeUseCase
{
    Task<LeaveTypeResponse> ExecuteAsync(Guid id, UpdateLeaveTypeRequest request, Guid organizationId, Guid currentUserId, CancellationToken cancellationToken = default);
}
