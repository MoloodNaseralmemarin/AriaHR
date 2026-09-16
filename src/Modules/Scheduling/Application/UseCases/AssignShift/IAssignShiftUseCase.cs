using AriaHR.Modules.Scheduling.Application.DTOs;

namespace AriaHR.Modules.Scheduling.Application.UseCases.AssignShift;

public interface IAssignShiftUseCase
{
    Task<ShiftAssignmentDto> ExecuteAsync(
        AssignShiftRequest request,
        Guid targetOrganizationId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
}
