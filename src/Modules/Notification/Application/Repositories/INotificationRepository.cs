using AriaHR.Modules.Notification.Domain.Entities;

namespace AriaHR.Modules.Notification.Application.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Domain.Entities.Notification notification, CancellationToken cancellationToken = default);
    Task<Domain.Entities.Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Domain.Entities.Notification?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.Notification>> GetActiveInboxForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.Notification>> GetHistoryForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.Notification>> GetSentByUserAsync(Guid senderUserId, CancellationToken cancellationToken = default);
    Task<NotificationRecipient?> GetRecipientAsync(Guid notificationId, Guid recipientUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationRecipient>> GetRecipientsAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task AddReplyAsync(NotificationReply reply, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationReply>> GetRepliesForThreadAsync(Guid notificationId, Guid recipientUserId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Domain.Entities.Notification notification, CancellationToken cancellationToken = default);
    Task UpdateRecipientAsync(NotificationRecipient recipient, CancellationToken cancellationToken = default);
}
