using AriaHR.Modules.Requests.Application.DTOs;

namespace AriaHR.Modules.Requests.Application.UseCases.GetLeaveRequestById;

public interface IGetLeaveRequestByIdUseCase
{
    Task<LeaveRequestDto?> ExecuteAsync(
        Guid id,
        Guid userId,
        bool isSystemAdmin,
        bool isCenterManager,
        Guid? userOrganizationId,
        CancellationToken cancellationToken = default);
}
