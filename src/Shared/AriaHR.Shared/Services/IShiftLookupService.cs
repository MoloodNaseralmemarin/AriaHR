namespace AriaHR.Shared.Services;

public record EmployeeShiftAssignmentDto(
    Guid ShiftId,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsShiftActive,
    string AssignmentStatus
);

public interface IShiftLookupService
{
    Task<EmployeeShiftAssignmentDto?> GetShiftAssignmentForEmployeeOnDateAsync(
        Guid employeeId,
        DateOnly localDate,
        CancellationToken cancellationToken = default);

    Task<bool> HasAssignmentOnDateAsync(
        Guid employeeId,
        DateOnly localDate,
        CancellationToken cancellationToken = default);
}
