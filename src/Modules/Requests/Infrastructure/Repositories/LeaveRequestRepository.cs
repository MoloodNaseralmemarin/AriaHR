using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Domain.Enums;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Requests.Infrastructure.Repositories;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly RequestsDbContext _dbContext;

    public LeaveRequestRepository(RequestsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        await _dbContext.LeaveRequests.AddAsync(leaveRequest, cancellationToken);
    }

    public async Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetOrganizationRequestsAsync(
        Guid organizationId,
        RequestStatus? status = null,
        Guid? employeeId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && !r.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (employeeId.HasValue && employeeId.Value != Guid.Empty)
        {
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(r => r.EndDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.StartDate <= to.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        _dbContext.LeaveRequests.Update(leaveRequest);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public bool IsRelational()
    {
        return _dbContext.Database.IsRelational();
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.IsRelational() && _dbContext.Database.CurrentTransaction == null)
        {
            await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.CurrentTransaction != null)
        {
            await _dbContext.Database.CommitTransactionAsync(cancellationToken);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.CurrentTransaction != null)
        {
            await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
        }
    }

    public async Task<bool> HasConflictingLeaveRequestAsync(
        Guid employeeId,
        Guid organizationId,
        LeaveType leaveType,
        DateOnly startDate,
        DateOnly endDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        Guid? excludeRequestId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LeaveRequests
            .Where(r => !r.IsDeleted &&
                        r.EmployeeId == employeeId &&
                        r.OrganizationId == organizationId &&
                        (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approved));

        if (excludeRequestId.HasValue && excludeRequestId.Value != Guid.Empty)
        {
            query = query.Where(r => r.Id != excludeRequestId.Value);
        }

        if (leaveType == LeaveType.Daily)
        {
            // Daily leave covers the entire range [startDate, endDate]
            // Any existing daily or hourly request that falls within [startDate, endDate] conflicts.
            query = query.Where(r => r.StartDate <= endDate && r.EndDate >= startDate);
            return await query.AnyAsync(cancellationToken);
        }
        else // LeaveType.Hourly
        {
            var reqStartTime = startTime ?? TimeOnly.MinValue;
            var reqEndTime = endTime ?? TimeOnly.MaxValue;

            // Hourly leave occurs on startDate (which equals endDate) during [startTime, endTime].
            // It conflicts with:
            // 1. Any existing Daily leave that includes startDate (r.StartDate <= startDate && r.EndDate >= startDate)
            // 2. Any existing Hourly leave on startDate whose time interval overlaps with [startTime, endTime]
            //    (i.e. r.StartTime < endTime && r.EndTime > startTime)

            query = query.Where(r => r.StartDate <= startDate && r.EndDate >= startDate)
                         .Where(r => r.LeaveType == LeaveType.Daily ||
                                    (r.LeaveType == LeaveType.Hourly &&
                                     r.StartTime.HasValue && r.EndTime.HasValue &&
                                     r.StartTime.Value < reqEndTime && r.EndTime.Value > reqStartTime));

            return await query.AnyAsync(cancellationToken);
        }
    }
}
