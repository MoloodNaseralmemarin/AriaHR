using AriaHR.Modules.Scheduling.Application.DTOs;

namespace AriaHR.Modules.Scheduling.Application.UseCases.GetShiftCalendar;

public interface IGetShiftCalendarUseCase
{
    Task<IReadOnlyList<ShiftAssignmentDto>> ExecuteAsync(
        Guid organizationId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
