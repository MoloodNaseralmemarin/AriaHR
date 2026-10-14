using AriaHR.Modules.Notification.Application.Repositories;
using AriaHR.Modules.Notification.Domain.Entities;
using AriaHR.Modules.Notification.Domain.Enums;
using AriaHR.Modules.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Notification.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _dbContext;

    public NotificationRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Domain.Entities.Notification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Domain.Entities.Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<Domain.Entities.Notification?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Include(n => n.Recipients)
            .Include(n => n.Replies)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Entities.Notification>> GetActiveInboxForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Include(n => n.Recipients)
            .Where(n => n.Recipients.Any(r => r.RecipientUserId == userId))
            .Where(n => n.Status == PublicationStatus.Published)
            .Where(n => n.PublishStartAtUtc <= nowUtc)
            .Where(n => n.ExpiresAtUtc == null || n.ExpiresAtUtc > nowUtc)
            .OrderByDescending(n => n.Priority)
            .ThenByDescending(n => n.PublishStartAtUtc)
            .ThenByDescending(n => n.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Entities.Notification>> GetHistoryForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Include(n => n.Recipients)
            .Where(n => n.Recipients.Any(r => r.RecipientUserId == userId))
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Entities.Notification>> GetSentByUserAsync(Guid senderUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Include(n => n.Recipients)
            .Where(n => n.SenderUserId == senderUserId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationRecipient?> GetRecipientAsync(Guid notificationId, Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationRecipients
            .FirstOrDefaultAsync(r => r.NotificationId == notificationId && r.RecipientUserId == recipientUserId, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationRecipient>> GetRecipientsAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationRecipients
            .Where(r => r.NotificationId == notificationId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddReplyAsync(NotificationReply reply, CancellationToken cancellationToken = default)
    {
        await _dbContext.NotificationReplies.AddAsync(reply, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationReply>> GetRepliesForThreadAsync(Guid notificationId, Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationReplies
            .Where(r => r.NotificationId == notificationId && r.RecipientUserId == recipientUserId)
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Domain.Entities.Notification notification, CancellationToken cancellationToken = default)
    {
        _dbContext.Notifications.Update(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRecipientAsync(NotificationRecipient recipient, CancellationToken cancellationToken = default)
    {
        _dbContext.NotificationRecipients.Update(recipient);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
