using AriaHR.Modules.Notification.Application.Repositories;
using AriaHR.Modules.Notification.Domain.Entities;
using AriaHR.Modules.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Notification.Infrastructure.Repositories;

public class CrossCenterPermissionRepository : ICrossCenterPermissionRepository
{
    private readonly NotificationDbContext _dbContext;

    public CrossCenterPermissionRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(CrossCenterPermission permission, CancellationToken cancellationToken = default)
    {
        await _dbContext.CrossCenterPermissions.AddAsync(permission, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CrossCenterPermission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CrossCenterPermissions
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<CrossCenterPermission?> GetActivePermissionAsync(Guid sourceOrgId, Guid destOrgId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CrossCenterPermissions
            .FirstOrDefaultAsync(p => p.SourceOrganizationId == sourceOrgId && p.DestinationOrganizationId == destOrgId && p.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<CrossCenterPermission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CrossCenterPermissions
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(CrossCenterPermission permission, CancellationToken cancellationToken = default)
    {
        _dbContext.CrossCenterPermissions.Update(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
