using AriaHR.Shared;

namespace AriaHR.Modules.Notification.Domain.Entities;

/// <summary>
/// Domain entity representing an intended recipient of a notification.
/// Maintains individual delivery, read, and acknowledgment states per recipient.
/// </summary>
public class NotificationRecipient : BaseEntity
{
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;
    public Guid RecipientUserId { get; set; }
    public Guid? RecipientOrganizationId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
}
