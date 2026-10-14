using AriaHR.Modules.Notification.Application.DTOs;
using AriaHR.Modules.Notification.Application.Repositories;
using AriaHR.Modules.Notification.Application.Services;
using AriaHR.Modules.Notification.Domain.Entities;
using AriaHR.Modules.Notification.Domain.Enums;
using AriaHR.Shared.Services;

namespace AriaHR.Modules.Notification.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICrossCenterPermissionRepository _crossCenterPermissionRepository;
    private readonly IUserDeviceRepository _userDeviceRepository;
    private readonly ICurrentUserService _currentUserService;

    public NotificationService(
        INotificationRepository notificationRepository,
        ICrossCenterPermissionRepository crossCenterPermissionRepository,
        IUserDeviceRepository userDeviceRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _crossCenterPermissionRepository = crossCenterPermissionRepository;
        _userDeviceRepository = userDeviceRepository;
        _currentUserService = currentUserService;
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        // Validate Title and Message
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("عنوان پیام نمی‌تواند خالی باشد.", nameof(request.Title));

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("متن پیام نمی‌تواند خالی باشد.", nameof(request.Message));

        // Validate Enum values
        if (!Enum.IsDefined(typeof(MessageType), request.Type))
            throw new ArgumentException("نوع پیام معتبر نیست.", nameof(request.Type));

        if (!Enum.IsDefined(typeof(MessagePriority), request.Priority))
            throw new ArgumentException("اولویت پیام معتبر نیست.", nameof(request.Priority));

        // Validate Publication Window
        if (request.ExpiresAtUtc.HasValue && request.ExpiresAtUtc.Value <= request.PublishStartAtUtc)
            throw new ArgumentException("تاریخ انقضا باید بعد از تاریخ شروع انتشار باشد.", nameof(request.ExpiresAtUtc));

        // Validate Recipients list
        if (request.RecipientUserIds == null || !request.RecipientUserIds.Any())
            throw new ArgumentException("حداقل یک گیرنده برای پیام باید مشخص شود.", nameof(request.RecipientUserIds));

        var currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("کاربر معتبر نیست.");

        var isSystemAdmin = _currentUserService.IsInRole("SystemAdmin");
        var isCenterManager = _currentUserService.IsInRole("CenterManager");
        var isEmployee = _currentUserService.IsInRole("Employee");

        var senderOrgId = _currentUserService.OrganizationId;

        // Determine destination organization ID
        Guid? destOrgId = request.RecipientOrganizationId ?? senderOrgId;

        // Authorization checks according to Section 2.1 Communication Rules
        if (isSystemAdmin)
        {
            // SystemAdmin -> CenterManager
            // SystemAdmin can send to any CenterManager across organizations.
        }
        else if (isCenterManager)
        {
            if (!senderOrgId.HasValue || senderOrgId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("سازمان مدیر مرکز مشخص نیست.");

            // If sending to a different center, inspect CrossCenterPermission
            if (destOrgId.HasValue && destOrgId.Value != senderOrgId.Value)
            {
                var perm = await _crossCenterPermissionRepository.GetActivePermissionAsync(senderOrgId.Value, destOrgId.Value, cancellationToken);
                if (perm == null || !perm.IsActive)
                {
                    throw new UnauthorizedAccessException("مجوز ارتباط بین مرکزها توسط مدیر سیستم ثبت نشده است.");
                }
            }
        }
        else if (isEmployee)
        {
            // Employee -> CenterManager within authorized organization
            if (destOrgId.HasValue && senderOrgId.HasValue && destOrgId.Value != senderOrgId.Value)
            {
                throw new UnauthorizedAccessException("کارمند تنها مجاز به ارسال پیام به مدیر مرکز خود است.");
            }
        }

        var notification = new Domain.Entities.Notification
        {
            Id = Guid.NewGuid(),
            SenderUserId = currentUserId,
            SenderOrganizationId = senderOrgId,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Type = request.Type,
            Priority = request.Priority,
            Status = PublicationStatus.Published,
            RequiresAcknowledgment = request.RequiresAcknowledgment,
            PublishStartAtUtc = request.PublishStartAtUtc,
            ExpiresAtUtc = request.ExpiresAtUtc,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityType = request.RelatedEntityType,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = currentUserId
        };

        // Distinct recipients
        var distinctRecipients = request.RecipientUserIds.Distinct().ToList();
        foreach (var recipientUserId in distinctRecipients)
        {
            notification.Recipients.Add(new NotificationRecipient
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.Id,
                RecipientUserId = recipientUserId,
                RecipientOrganizationId = destOrgId,
                IsRead = false,
                IsAcknowledged = false,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = currentUserId
            });
        }

        await _notificationRepository.AddAsync(notification, cancellationToken);

        return MapToDto(notification, currentUserId);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetActiveInboxAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("کاربر معتبر نیست.");

        var nowUtc = DateTime.UtcNow;
        var notifications = await _notificationRepository.GetActiveInboxForUserAsync(currentUserId, nowUtc, cancellationToken);

        return notifications.Select(n => MapToDto(n, currentUserId)).ToList();
    }

    public async Task<IReadOnlyList<NotificationDto>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("کاربر معتبر نیست.");

        var notifications = await _notificationRepository.GetHistoryForUserAsync(currentUserId, cancellationToken);

        return notifications.Select(n => MapToDto(n, currentUserId)).ToList();
    }

    public async Task<IReadOnlyList<NotificationDto>> GetSentAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("کاربر معتبر نیست.");

        var notifications = await _notificationRepository.GetSentByUserAsync(currentUserId, cancellationToken);

        return notifications.Select(n => MapToDto(n, currentUserId)).ToList();
    }

    public async Task<NotificationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("کاربر معتبر نیست.");

        var notification = await _notificationRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (notification == null) return null;

        var isSender = notification.SenderUserId == currentUserId;
        var isRecipient = notification.Recipients.Any(r => r.RecipientUserId == currentUserId);
        var isSystemAdmin = _currentUserService.IsInRole("SystemAdmin");

        if (!isSender && !isRecipient && !isSystemAdmin)
        {
            throw new UnauthorizedAccessException("شما دسترسی به مشاهده این پیام را ندارید.");
        }

        return MapToDto(notification, currentUserId);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("کاربر معتبر نیست.");

        var recipient = await _notificationRepository.GetRecipientAsync(id, currentUserId, cancellationToken);
        if (recipient == null)
            throw new KeyNotFoundException("گیرنده برای این پیام یافت نشد.");

        if (!recipient.IsRead)
        {
            recipient.IsRead = true;
            recipient.ReadAtUtc = DateTime.UtcNow;
            recipient.UpdatedAtUtc = DateTime.UtcNow;
            recipient.UpdatedByUserId = currentUserId;

            await _notificationRepository.UpdateRecipientAsync(recipient, cancellationToken);
        }
    }

    public async Task AcknowledgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("کاربر معتبر نیست.");

        var notification = await _notificationRepository.GetByIdAsync(id, cancellationToken);
        if (notification == null)
            throw new KeyNotFoundException("پیام یافت نشد.");

        if (!notification.RequiresAcknowledgment)
            throw new InvalidOperationException("این پیام نیازمند تایید رسمی دریافت نیست.");

        var recipient = await _notificationRepository.GetRecipientAsync(id, currentUserId, cancellationToken);
        if (recipient == null)
            throw new KeyNotFoundException("شما گیرنده این پیام نیستید و امکان تایید دریافت را ندارید.");

        if (!recipient.IsAcknowledged)
        {
            recipient.IsAcknowledged = true;
            recipient.AcknowledgedAtUtc = DateTime.UtcNow;
            if (!recipient.IsRead)
            {
                recipient.IsRead = true;
                recipient.ReadAtUtc = DateTime.UtcNow;
            }
            recipient.UpdatedAtUtc = DateTime.UtcNow;
            recipient.UpdatedByUserId = currentUserId;

            await _notificationRepository.UpdateRecipientAsync(recipient, cancellationToken);
        }
    }

    public async Task UpdatePublicationStatusAsync(Guid id, UpdatePublicationStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(PublicationStatus), request.Status))
            throw new ArgumentException("وضعیت انتشار معتبر نیست.", nameof(request.Status));

        var currentUserId = _currentUserService.UserId;
        var notification = await _notificationRepository.GetByIdAsync(id, cancellationToken);
        if (notification == null)
            throw new KeyNotFoundException("پیام یافت نشد.");

        var isSender = notification.SenderUserId == currentUserId;
        var isSystemAdmin = _currentUserService.IsInRole("SystemAdmin");

        if (!isSender && !isSystemAdmin)
            throw new UnauthorizedAccessException("تنها فرستنده پیام یا مدیر سیستم می‌تواند وضعیت انتشار پیام را تغییر دهد.");

        notification.Status = request.Status;
        notification.UpdatedAtUtc = DateTime.UtcNow;
        notification.UpdatedByUserId = currentUserId;

        await _notificationRepository.UpdateAsync(notification, cancellationToken);
    }

    public async Task<NotificationReplyDto> CreateReplyAsync(Guid notificationId, CreateReplyRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("متن پاسخ نمی‌تواند خالی باشد.", nameof(request.Content));

        var currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("کاربر معتبر نیست.");

        var notification = await _notificationRepository.GetByIdWithDetailsAsync(notificationId, cancellationToken);
        if (notification == null)
            throw new KeyNotFoundException("پیام یافت نشد.");

        var isSender = notification.SenderUserId == currentUserId;
        var isTargetRecipient = notification.Recipients.Any(r => r.RecipientUserId == currentUserId);

        // Authorization: Current user must be either the original notification sender replying to a target recipient,
        // or the target recipient replying in their thread.
        Guid threadRecipientUserId;
        if (isSender)
        {
            // Original sender replying in a recipient's thread
            if (request.TargetRecipientUserId == Guid.Empty)
                throw new ArgumentException("گیرنده هدف پاسخ باید مشخص شود.", nameof(request.TargetRecipientUserId));

            if (!notification.Recipients.Any(r => r.RecipientUserId == request.TargetRecipientUserId))
                throw new ArgumentException("کاربر مشخص شده گیرنده این پیام نیست.", nameof(request.TargetRecipientUserId));

            threadRecipientUserId = request.TargetRecipientUserId;
        }
        else if (isTargetRecipient)
        {
            // Recipient replying to sender
            threadRecipientUserId = currentUserId;
        }
        else
        {
            throw new UnauthorizedAccessException("شما مجاز به ثبت پاسخ در این گفتگو نیستید.");
        }

        var reply = new NotificationReply
        {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            RecipientUserId = threadRecipientUserId,
            SenderUserId = currentUserId,
            Content = request.Content.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = currentUserId
        };

        await _notificationRepository.AddReplyAsync(reply, cancellationToken);

        return new NotificationReplyDto
        {
            Id = reply.Id,
            NotificationId = reply.NotificationId,
            RecipientUserId = reply.RecipientUserId,
            SenderUserId = reply.SenderUserId,
            Content = reply.Content,
            CreatedAtUtc = reply.CreatedAtUtc
        };
    }

    public async Task<IReadOnlyList<NotificationReplyDto>> GetRepliesForThreadAsync(Guid notificationId, Guid targetRecipientUserId, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("کاربر معتبر نیست.");

        var notification = await _notificationRepository.GetByIdWithDetailsAsync(notificationId, cancellationToken);
        if (notification == null)
            throw new KeyNotFoundException("پیام یافت نشد.");

        var isSender = notification.SenderUserId == currentUserId;

        Guid threadRecipientUserId;
        if (isSender)
        {
            // Original sender looking at recipient's thread
            threadRecipientUserId = targetRecipientUserId != Guid.Empty ? targetRecipientUserId : currentUserId;
        }
        else
        {
            // Recipient can ONLY look at their own thread
            threadRecipientUserId = currentUserId;
        }

        var replies = await _notificationRepository.GetRepliesForThreadAsync(notificationId, threadRecipientUserId, cancellationToken);

        return replies.Select(r => new NotificationReplyDto
        {
            Id = r.Id,
            NotificationId = r.NotificationId,
            RecipientUserId = r.RecipientUserId,
            SenderUserId = r.SenderUserId,
            Content = r.Content,
            CreatedAtUtc = r.CreatedAtUtc
        }).ToList();
    }

    public async Task<IReadOnlyList<NotificationRecipientDto>> GetRecipientsForNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);
        if (notification == null)
            throw new KeyNotFoundException("پیام یافت نشد.");

        var isSender = notification.SenderUserId == currentUserId;
        var isSystemAdmin = _currentUserService.IsInRole("SystemAdmin");

        if (!isSender && !isSystemAdmin)
            throw new UnauthorizedAccessException("تنها فرستنده پیام یا مدیر سیستم می‌تواند وضعیت گیرندگان را مشاهده کند.");

        var recipients = await _notificationRepository.GetRecipientsAsync(notificationId, cancellationToken);

        return recipients.Select(r => new NotificationRecipientDto
        {
            Id = r.Id,
            NotificationId = r.NotificationId,
            RecipientUserId = r.RecipientUserId,
            RecipientOrganizationId = r.RecipientOrganizationId,
            IsRead = r.IsRead,
            ReadAtUtc = r.ReadAtUtc,
            IsAcknowledged = r.IsAcknowledged,
            AcknowledgedAtUtc = r.AcknowledgedAtUtc
        }).ToList();
    }

    public async Task<CrossCenterPermissionDto> GrantCrossCenterPermissionAsync(GrantCrossCenterPermissionRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsInRole("SystemAdmin"))
            throw new UnauthorizedAccessException("تنها مدیر سیستم مجاز به اعطای مجوزهای بین مرکز است.");

        if (request.SourceOrganizationId == Guid.Empty || request.DestinationOrganizationId == Guid.Empty)
            throw new ArgumentException("شناسه سازمان مبدا و مقصد معتبر نیست.");

        if (request.SourceOrganizationId == request.DestinationOrganizationId)
            throw new ArgumentException("سازمان مبدا و مقصد نمی‌توانند یکسان باشند.");

        var currentUserId = _currentUserService.UserId;

        var existing = await _crossCenterPermissionRepository.GetActivePermissionAsync(request.SourceOrganizationId, request.DestinationOrganizationId, cancellationToken);
        if (existing != null)
        {
            existing.IsActive = true;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            existing.UpdatedByUserId = currentUserId;
            await _crossCenterPermissionRepository.UpdateAsync(existing, cancellationToken);

            return MapCrossCenterPermissionToDto(existing);
        }

        var permission = new CrossCenterPermission
        {
            Id = Guid.NewGuid(),
            SourceOrganizationId = request.SourceOrganizationId,
            DestinationOrganizationId = request.DestinationOrganizationId,
            GrantedByUserId = currentUserId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = currentUserId
        };

        await _crossCenterPermissionRepository.AddAsync(permission, cancellationToken);

        return MapCrossCenterPermissionToDto(permission);
    }

    public async Task RevokeCrossCenterPermissionAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsInRole("SystemAdmin"))
            throw new UnauthorizedAccessException("تنها مدیر سیستم مجاز به لغو مجوزهای بین مرکز است.");

        var permission = await _crossCenterPermissionRepository.GetByIdAsync(permissionId, cancellationToken);
        if (permission == null)
            throw new KeyNotFoundException("مجوز یافت نشد.");

        permission.IsActive = false;
        permission.UpdatedAtUtc = DateTime.UtcNow;
        permission.UpdatedByUserId = _currentUserService.UserId;

        await _crossCenterPermissionRepository.UpdateAsync(permission, cancellationToken);
    }

    public async Task<IReadOnlyList<CrossCenterPermissionDto>> GetCrossCenterPermissionsAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsInRole("SystemAdmin"))
            throw new UnauthorizedAccessException("تنها مدیر سیستم مجاز به مشاهده مجوزهای بین مرکز است.");

        var permissions = await _crossCenterPermissionRepository.GetAllAsync(cancellationToken);
        return permissions.Select(MapCrossCenterPermissionToDto).ToList();
    }

    public async Task RegisterDeviceAsync(RegisterDeviceRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceToken))
            throw new ArgumentException("توکن دستگاه نمی‌تواند خالی باشد.", nameof(request.DeviceToken));

        var currentUserId = _currentUserService.UserId;
        var currentOrgId = _currentUserService.OrganizationId ?? Guid.Empty;

        var existing = await _userDeviceRepository.GetByDeviceTokenAsync(request.DeviceToken, cancellationToken);
        if (existing != null)
        {
            existing.UserId = currentUserId;
            existing.OrganizationId = currentOrgId;
            existing.DeviceType = request.DeviceType;
            existing.IsActive = true;
            existing.LastActiveDate = DateTime.UtcNow;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            existing.UpdatedByUserId = currentUserId;

            await _userDeviceRepository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var device = new UserDevice
            {
                Id = Guid.NewGuid(),
                OrganizationId = currentOrgId,
                UserId = currentUserId,
                DeviceToken = request.DeviceToken,
                DeviceType = request.DeviceType,
                IsActive = true,
                LastActiveDate = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = currentUserId
            };

            await _userDeviceRepository.AddAsync(device, cancellationToken);
        }
    }

    public async Task UnregisterDeviceAsync(string deviceToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
            throw new ArgumentException("توکن دستگاه نمی‌تواند خالی باشد.", nameof(deviceToken));

        var device = await _userDeviceRepository.GetByDeviceTokenAsync(deviceToken, cancellationToken);
        if (device != null)
        {
            device.IsActive = false;
            device.UpdatedAtUtc = DateTime.UtcNow;
            device.UpdatedByUserId = _currentUserService.UserId;

            await _userDeviceRepository.UpdateAsync(device, cancellationToken);
        }
    }

    private static NotificationDto MapToDto(Domain.Entities.Notification notification, Guid currentUserId)
    {
        var recipientRecord = notification.Recipients.FirstOrDefault(r => r.RecipientUserId == currentUserId);

        return new NotificationDto
        {
            Id = notification.Id,
            SenderUserId = notification.SenderUserId,
            SenderOrganizationId = notification.SenderOrganizationId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Priority = notification.Priority,
            Status = notification.Status,
            RequiresAcknowledgment = notification.RequiresAcknowledgment,
            PublishStartAtUtc = notification.PublishStartAtUtc,
            ExpiresAtUtc = notification.ExpiresAtUtc,
            IsRead = recipientRecord?.IsRead,
            ReadAtUtc = recipientRecord?.ReadAtUtc,
            IsAcknowledged = recipientRecord?.IsAcknowledged,
            AcknowledgedAtUtc = recipientRecord?.AcknowledgedAtUtc,
            CreatedAtUtc = notification.CreatedAtUtc
        };
    }

    private static CrossCenterPermissionDto MapCrossCenterPermissionToDto(CrossCenterPermission permission)
    {
        return new CrossCenterPermissionDto
        {
            Id = permission.Id,
            SourceOrganizationId = permission.SourceOrganizationId,
            DestinationOrganizationId = permission.DestinationOrganizationId,
            GrantedByUserId = permission.GrantedByUserId,
            IsActive = permission.IsActive,
            CreatedAtUtc = permission.CreatedAtUtc
        };
    }
}
