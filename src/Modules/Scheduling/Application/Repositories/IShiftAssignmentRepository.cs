using AriaHR.Modules.Scheduling.Domain.Entities;

namespace AriaHR.Modules.Scheduling.Application.Repositories;

public interface IShiftAssignmentRepository
{
    Task AddAsync(ShiftAssignment shiftAssignment, CancellationToken cancellationToken = default);
    Task<ShiftAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftAssignment>> GetByEmployeeAndDateRangeAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftAssignment>> GetCalendarAssignmentsAsync(
        Guid organizationId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
    Task<bool> HasAssignmentOnDateAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
