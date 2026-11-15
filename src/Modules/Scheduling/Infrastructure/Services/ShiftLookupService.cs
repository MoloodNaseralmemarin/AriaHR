using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using AriaHR.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Scheduling.Infrastructure.Services;

public class ShiftLookupService : IShiftLookupService
{
    private readonly SchedulingDbContext _dbContext;

    public ShiftLookupService(SchedulingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<EmployeeShiftAssignmentDto?> GetShiftAssignmentForEmployeeOnDateAsync(
        Guid employeeId,
        DateOnly localDate,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return null;
        }

        var assignment = await _dbContext.ShiftAssignments
            .AsNoTracking()
            .Include(sa => sa.Shift)
            .Where(sa => !sa.IsDeleted && sa.EmployeeId == employeeId && sa.Date == localDate)
            .OrderByDescending(sa => sa.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment?.Shift == null || assignment.Shift.IsDeleted)
        {
            return null;
        }

        return new EmployeeShiftAssignmentDto(
            assignment.Shift.Id,
            assignment.Shift.Name,
            assignment.Shift.StartTime,
            assignment.Shift.EndTime,
            assignment.Shift.IsActive,
            assignment.Status);
    }

    public async Task<bool> HasAssignmentOnDateAsync(
        Guid employeeId,
        DateOnly localDate,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return false;
        }

        return await _dbContext.ShiftAssignments
            .AsNoTracking()
            .AnyAsync(sa => !sa.IsDeleted && sa.EmployeeId == employeeId && sa.Date == localDate, cancellationToken);
    }
}
