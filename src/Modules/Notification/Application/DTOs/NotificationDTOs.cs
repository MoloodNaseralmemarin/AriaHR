using AriaHR.Modules.Notification.Domain.Enums;

namespace AriaHR.Modules.Notification.Application.DTOs;

public class CreateNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Information;
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;
    public bool RequiresAcknowledgment { get; set; }
    public DateTime PublishStartAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public List<Guid> RecipientUserIds { get; set; } = new();
    public Guid? RecipientOrganizationId { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid SenderUserId { get; set; }
    public Guid? SenderOrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public MessagePriority Priority { get; set; }
    public PublicationStatus Status { get; set; }
    public bool RequiresAcknowledgment { get; set; }
    public DateTime PublishStartAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool? IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public bool? IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class NotificationRecipientDto
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public Guid RecipientUserId { get; set; }
    public Guid? RecipientOrganizationId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
}

public class CreateReplyRequest
{
    public Guid TargetRecipientUserId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class NotificationReplyDto
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public Guid RecipientUserId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class UpdatePublicationStatusRequest
{
    public PublicationStatus Status { get; set; }
}

public class GrantCrossCenterPermissionRequest
{
    public Guid SourceOrganizationId { get; set; }
    public Guid DestinationOrganizationId { get; set; }
}

public class CrossCenterPermissionDto
{
    public Guid Id { get; set; }
    public Guid SourceOrganizationId { get; set; }
    public Guid DestinationOrganizationId { get; set; }
    public Guid GrantedByUserId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class RegisterDeviceRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
}

public class UserDeviceDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastActiveDate { get; set; }
}
