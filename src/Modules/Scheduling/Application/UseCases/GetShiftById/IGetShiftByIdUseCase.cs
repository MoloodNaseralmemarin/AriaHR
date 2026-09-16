using AriaHR.Modules.Scheduling.Application.DTOs;

namespace AriaHR.Modules.Scheduling.Application.UseCases.GetShiftById;

public interface IGetShiftByIdUseCase
{
    Task<ShiftDto?> ExecuteAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
