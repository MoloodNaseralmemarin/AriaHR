using AriaHR.Modules.Scheduling.Application.Repositories;
using AriaHR.Modules.Scheduling.Domain.Entities;
using AriaHR.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Scheduling.Infrastructure.Repositories;

public class ShiftAssignmentRepository : IShiftAssignmentRepository
{
    private readonly SchedulingDbContext _dbContext;

    public ShiftAssignmentRepository(SchedulingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(ShiftAssignment shiftAssignment, CancellationToken cancellationToken = default)
    {
        await _dbContext.ShiftAssignments.AddAsync(shiftAssignment, cancellationToken);
    }

    public async Task<ShiftAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShiftAssignments
            .AsNoTracking()
            .Include(sa => sa.Shift)
            .FirstOrDefaultAsync(sa => sa.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftAssignment>> GetByEmployeeAndDateRangeAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShiftAssignments
            .AsNoTracking()
            .Include(sa => sa.Shift)
            .Where(sa => sa.EmployeeId == employeeId && sa.Date >= startDate && sa.Date <= endDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftAssignment>> GetCalendarAssignmentsAsync(
        Guid organizationId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ShiftAssignments
            .AsNoTracking()
            .Include(sa => sa.Shift)
            .Where(sa => sa.OrganizationId == organizationId && sa.Date >= startDate && sa.Date <= endDate);

        if (employeeId.HasValue)
        {
            query = query.Where(sa => sa.EmployeeId == employeeId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> HasAssignmentOnDateAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShiftAssignments
            .AsNoTracking()
            .AnyAsync(sa => sa.EmployeeId == employeeId && sa.Date == date, cancellationToken);
    }
}
