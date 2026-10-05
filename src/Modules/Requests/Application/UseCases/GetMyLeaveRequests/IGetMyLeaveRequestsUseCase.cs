using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.GetMyLeaveRequests;

public interface IGetMyLeaveRequestsUseCase
{
    Task<IEnumerable<LeaveRequestDto>> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default);
}
