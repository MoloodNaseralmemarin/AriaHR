using AriaHR.Modules.Notification.Application.DTOs;

namespace AriaHR.Modules.Notification.Application.Services;

public interface INotificationService
{
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> GetActiveInboxAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> GetSentAsync(CancellationToken cancellationToken = default);
    Task<NotificationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task AcknowledgeAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdatePublicationStatusAsync(Guid id, UpdatePublicationStatusRequest request, CancellationToken cancellationToken = default);
    Task<NotificationReplyDto> CreateReplyAsync(Guid notificationId, CreateReplyRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationReplyDto>> GetRepliesForThreadAsync(Guid notificationId, Guid targetRecipientUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationRecipientDto>> GetRecipientsForNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<CrossCenterPermissionDto> GrantCrossCenterPermissionAsync(GrantCrossCenterPermissionRequest request, CancellationToken cancellationToken = default);
    Task RevokeCrossCenterPermissionAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CrossCenterPermissionDto>> GetCrossCenterPermissionsAsync(CancellationToken cancellationToken = default);
    Task RegisterDeviceAsync(RegisterDeviceRequest request, CancellationToken cancellationToken = default);
    Task UnregisterDeviceAsync(string deviceToken, CancellationToken cancellationToken = default);
}
