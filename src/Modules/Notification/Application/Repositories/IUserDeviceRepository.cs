using AriaHR.Modules.Notification.Domain.Entities;

namespace AriaHR.Modules.Notification.Application.Repositories;

public interface IUserDeviceRepository
{
    Task AddAsync(UserDevice userDevice, CancellationToken cancellationToken = default);
    Task<UserDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDevice>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDevice?> GetByUserIdAndDeviceTokenAsync(Guid userId, string deviceToken, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserDevice userDevice, CancellationToken cancellationToken = default);
}
