using AriaHR.Modules.Notification.Domain.Entities;

namespace AriaHR.Modules.Notification.Application.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Domain.Entities.Notification notification, CancellationToken cancellationToken = default);
    Task<Domain.Entities.Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Domain.Entities.Notification notification, CancellationToken cancellationToken = default);
}
