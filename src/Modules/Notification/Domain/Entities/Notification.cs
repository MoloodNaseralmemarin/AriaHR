using AriaHR.Modules.Notification.Domain.Enums;
using AriaHR.Shared;

namespace AriaHR.Modules.Notification.Domain.Entities;

/// <summary>
/// Domain entity representing a system notification/message sent by an authorized sender.
/// </summary>
public class Notification : BaseEntity
{
    public Guid SenderUserId { get; set; }
    public Guid? SenderOrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Information;
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;
    public PublicationStatus Status { get; set; } = PublicationStatus.Published;
    public bool RequiresAcknowledgment { get; set; }
    public DateTime PublishStartAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }

    public ICollection<NotificationRecipient> Recipients { get; set; } = new List<NotificationRecipient>();
    public ICollection<NotificationReply> Replies { get; set; } = new List<NotificationReply>();
}
