using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Domain.Entities;
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
        ArgumentNullException.ThrowIfNull(leaveRequest);
        await _dbContext.LeaveRequests.AddAsync(leaveRequest, cancellationToken);
    }

    public async Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(r => r.LeaveType)
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetPendingRequestsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(r => r.LeaveType)
            .Where(r => r.OrganizationId == organizationId && r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetPendingRequestsForApproverAsync(Guid approverId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(r => r.LeaveType)
            .Where(r => r.ApproverId == approverId && r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetApprovedRequestsByDateRangeAsync(
        Guid organizationId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(r => r.LeaveType)
            .Where(r => r.OrganizationId == organizationId
                        && r.Status == "Approved"
                        && r.FromDate <= endDate
                        && r.ToDate >= startDate)
            .OrderBy(r => r.FromDate)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(leaveRequest);
        _dbContext.LeaveRequests.Update(leaveRequest);
        return Task.CompletedTask;
    }
}
