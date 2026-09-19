using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.Repositories;

namespace AriaHR.Modules.Scheduling.Application.UseCases.GetEmployeeShiftCalendar;

public class GetEmployeeShiftCalendarUseCase : IGetEmployeeShiftCalendarUseCase
{
    private readonly IShiftAssignmentRepository _shiftAssignmentRepository;

    public GetEmployeeShiftCalendarUseCase(IShiftAssignmentRepository shiftAssignmentRepository)
    {
        _shiftAssignmentRepository = shiftAssignmentRepository ?? throw new ArgumentNullException(nameof(shiftAssignmentRepository));
    }

    public async Task<IReadOnlyList<ShiftAssignmentDto>> ExecuteAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid organizationId = default,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("شناسه کارمند الزامی است.", nameof(employeeId));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("تاریخ پایان نباید قبل از تاریخ شروع باشد.");
        }

        var assignments = await _shiftAssignmentRepository.GetByEmployeeAndDateRangeAsync(
            employeeId, startDate, endDate, cancellationToken);

        if (organizationId != Guid.Empty)
        {
            assignments = assignments.Where(a => a.OrganizationId == organizationId).ToList();
        }

        return assignments.Select(a => new ShiftAssignmentDto(
            a.Id,
            a.OrganizationId,
            a.EmployeeId,
            a.ShiftId,
            a.Shift?.Name,
            a.Shift?.StartTime,
            a.Shift?.EndTime,
            a.Date,
            a.Status,
            a.CreatedBy
        )).ToList();
    }
}
