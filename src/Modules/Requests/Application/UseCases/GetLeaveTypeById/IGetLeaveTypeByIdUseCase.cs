using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.GetLeaveTypeById;

public interface IGetLeaveTypeByIdUseCase
{
    Task<LeaveTypeResponse?> ExecuteAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
}
