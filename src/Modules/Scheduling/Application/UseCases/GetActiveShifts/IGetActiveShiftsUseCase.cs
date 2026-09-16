using AriaHR.Modules.Scheduling.Application.DTOs;

namespace AriaHR.Modules.Scheduling.Application.UseCases.GetActiveShifts;

public interface IGetActiveShiftsUseCase
{
    Task<IReadOnlyList<ShiftDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
