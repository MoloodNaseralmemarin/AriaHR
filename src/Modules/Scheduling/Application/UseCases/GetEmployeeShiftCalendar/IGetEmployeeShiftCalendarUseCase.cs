using AriaHR.Modules.Scheduling.Application.DTOs;

namespace AriaHR.Modules.Scheduling.Application.UseCases.GetEmployeeShiftCalendar;

public interface IGetEmployeeShiftCalendarUseCase
{
    Task<IReadOnlyList<ShiftAssignmentDto>> ExecuteAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid organizationId = default,
        CancellationToken cancellationToken = default);
}
