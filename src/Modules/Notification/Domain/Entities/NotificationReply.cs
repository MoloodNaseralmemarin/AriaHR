using AriaHR.Shared;

namespace AriaHR.Modules.Notification.Domain.Entities;

/// <summary>
/// Domain entity representing a private reply in a notification thread.
/// Replies are associated with the parent notification and a specific recipient's conversation thread.
/// </summary>
public class NotificationReply : BaseEntity
{
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;
    public Guid RecipientUserId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Content { get; set; } = string.Empty;
}
