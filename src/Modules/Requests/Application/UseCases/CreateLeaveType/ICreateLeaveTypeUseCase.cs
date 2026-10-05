using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.CreateLeaveType;

public interface ICreateLeaveTypeUseCase
{
    Task<LeaveTypeResponse> ExecuteAsync(CreateLeaveTypeRequest request, Guid organizationId, Guid currentUserId, CancellationToken cancellationToken = default);
}
