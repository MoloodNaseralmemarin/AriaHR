using AriaHR.Modules.Scheduling.Application.DTOs;

namespace AriaHR.Modules.Scheduling.Application.UseCases.DefineShift;

public interface IDefineShiftUseCase
{
    Task<ShiftDto> ExecuteAsync(
        DefineShiftRequest request,
        Guid targetOrganizationId,
        CancellationToken cancellationToken = default);
}
