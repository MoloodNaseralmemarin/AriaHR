using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Requests.Infrastructure.Repositories;

public class LeaveTypeRepository : ILeaveTypeRepository
{
    private readonly RequestsDbContext _dbContext;

    public LeaveTypeRepository(RequestsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task<LeaveType?> GetByIdAndOrganizationIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveTypes
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == organizationId && !t.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveType>> GetAllByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveTypes
            .AsNoTracking()
            .Where(t => t.OrganizationId == organizationId && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveType>> GetAllActiveAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveTypes
            .AsNoTracking()
            .Where(t => t.OrganizationId == organizationId && !t.IsDeleted && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(Guid organizationId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        string normalizedName = name.Trim().ToLower();
        return await _dbContext.LeaveTypes
            .AsNoTracking()
            .AnyAsync(t => t.OrganizationId == organizationId
                           && !t.IsDeleted
                           && t.Name.ToLower() == normalizedName
                           && (!excludeId.HasValue || t.Id != excludeId.Value),
                cancellationToken);
    }

    public async Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(leaveType);
        await _dbContext.LeaveTypes.AddAsync(leaveType, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LeaveType leaveType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(leaveType);
        _dbContext.LeaveTypes.Update(leaveType);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
