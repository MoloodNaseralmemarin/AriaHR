using AriaHR.Shared;

namespace AriaHR.Modules.Notification.Domain.Entities;

/// <summary>
/// UserDevice entity registering push notification tokens for user devices.
/// </summary>
public class UserDevice : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastActiveDate { get; set; }
}
