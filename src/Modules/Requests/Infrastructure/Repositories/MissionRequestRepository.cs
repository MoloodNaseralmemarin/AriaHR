using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Requests.Infrastructure.Repositories;

public class MissionRequestRepository : IMissionRequestRepository
{
    private readonly RequestsDbContext _dbContext;

    public MissionRequestRepository(RequestsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(MissionRequest missionRequest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(missionRequest);
        await _dbContext.MissionRequests.AddAsync(missionRequest, cancellationToken);
    }

    public async Task<MissionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionRequests
            .AsNoTracking()
            .Include(r => r.MissionLocationLogs)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MissionRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MissionRequest>> GetPendingRequestsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionRequests
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MissionRequest>> GetPendingRequestsForApproverAsync(Guid approverId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionRequests
            .AsNoTracking()
            .Where(r => r.ApproverId == approverId && r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MissionRequest>> GetApprovedRequestsByDateRangeAsync(
        Guid organizationId,
        DateTime startDateTime,
        DateTime endDateTime,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionRequests
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId
                        && r.Status == "Approved"
                        && r.FromDateTime <= endDateTime
                        && r.ToDateTime >= startDateTime)
            .OrderBy(r => r.FromDateTime)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(MissionRequest missionRequest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(missionRequest);
        _dbContext.MissionRequests.Update(missionRequest);
        return Task.CompletedTask;
    }
}
