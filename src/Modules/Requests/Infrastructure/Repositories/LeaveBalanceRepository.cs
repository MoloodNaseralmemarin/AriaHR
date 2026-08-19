using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Requests.Infrastructure.Repositories;

public class LeaveBalanceRepository : ILeaveBalanceRepository
{
    private readonly RequestsDbContext _dbContext;

    public LeaveBalanceRepository(RequestsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(LeaveBalance leaveBalance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(leaveBalance);
        await _dbContext.LeaveBalances.AddAsync(leaveBalance, cancellationToken);
    }

    public async Task<LeaveBalance?> GetByEmployeeAndLeaveTypeAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveBalances
            .AsNoTracking()
            .Include(b => b.LeaveType)
            .FirstOrDefaultAsync(
                b => b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId && b.Year == year,
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveBalance>> GetByEmployeeAndYearAsync(
        Guid employeeId,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveBalances
            .AsNoTracking()
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(LeaveBalance leaveBalance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(leaveBalance);
        _dbContext.LeaveBalances.Update(leaveBalance);
        return Task.CompletedTask;
    }
}
