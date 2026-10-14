using AriaHR.Modules.Notification.Domain.Entities;

namespace AriaHR.Modules.Notification.Application.Repositories;

public interface ICrossCenterPermissionRepository
{
    Task AddAsync(CrossCenterPermission permission, CancellationToken cancellationToken = default);
    Task<CrossCenterPermission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CrossCenterPermission?> GetActivePermissionAsync(Guid sourceOrgId, Guid destOrgId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CrossCenterPermission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(CrossCenterPermission permission, CancellationToken cancellationToken = default);
}
