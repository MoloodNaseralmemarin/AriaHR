using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.CreateLeaveRequest;

public interface ICreateLeaveRequestUseCase
{
    Task<LeaveRequestDto> ExecuteAsync(CreateLeaveRequestRequest request, Guid userId, CancellationToken cancellationToken = default);
}
