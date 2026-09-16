using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.Repositories;

namespace AriaHR.Modules.Scheduling.Application.UseCases.GetShiftCalendar;

public class GetShiftCalendarUseCase : IGetShiftCalendarUseCase
{
    private readonly IShiftAssignmentRepository _shiftAssignmentRepository;

    public GetShiftCalendarUseCase(IShiftAssignmentRepository shiftAssignmentRepository)
    {
        _shiftAssignmentRepository = shiftAssignmentRepository ?? throw new ArgumentNullException(nameof(shiftAssignmentRepository));
    }

    public async Task<IReadOnlyList<ShiftAssignmentDto>> ExecuteAsync(
        Guid organizationId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("تاریخ پایان نباید قبل از تاریخ شروع باشد.");
        }

        var assignments = await _shiftAssignmentRepository.GetCalendarAssignmentsAsync(
            organizationId, startDate, endDate, cancellationToken);

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
