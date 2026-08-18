using AriaHR.Shared;

namespace AriaHR.Modules.Notification.Domain.Entities;

/// <summary>
/// Notification entity representing in-app system notifications.
/// </summary>
public class Notification : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public DateTime CreatedAt { get; set; }
}
