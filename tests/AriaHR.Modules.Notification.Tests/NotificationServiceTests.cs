using AriaHR.Modules.Notification.Application.DTOs;
using AriaHR.Modules.Notification.Application.Repositories;
using AriaHR.Modules.Notification.Application.Services;
using AriaHR.Modules.Notification.Domain.Entities;
using AriaHR.Modules.Notification.Domain.Enums;
using AriaHR.Shared.Services;
using Moq;
using Xunit;

namespace AriaHR.Modules.Notification.Tests;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepoMock = new();
    private readonly Mock<ICrossCenterPermissionRepository> _permissionRepoMock = new();
    private readonly Mock<IUserDeviceRepository> _deviceRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private NotificationService CreateService()
    {
        return new NotificationService(
            _notificationRepoMock.Object,
            _permissionRepoMock.Object,
            _deviceRepoMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateNotification_InvalidEnumValues_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();
        var request = new CreateNotificationRequest
        {
            Title = "Test",
            Message = "Test Message",
            Type = (MessageType)999, // Invalid enum
            Priority = MessagePriority.Normal,
            RecipientUserIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateNotificationAsync(request));
        Assert.Contains("نوع پیام معتبر نیست", ex.Message);
    }

    [Fact]
    public async Task CreateNotification_InvalidExpirationDate_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();
        var now = DateTime.UtcNow;
        var request = new CreateNotificationRequest
        {
            Title = "Test",
            Message = "Test Message",
            Type = MessageType.Information,
            Priority = MessagePriority.Normal,
            PublishStartAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(-5), // Expiration before start
            RecipientUserIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateNotificationAsync(request));
        Assert.Contains("تاریخ انقضا باید بعد از تاریخ شروع انتشار باشد", ex.Message);
    }

    [Fact]
    public async Task CreateNotification_CenterManagerCrossCenter_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var senderUserId = Guid.NewGuid();
        var senderOrgId = Guid.NewGuid();
        var destOrgId = Guid.NewGuid();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(senderUserId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(senderOrgId);
        _currentUserServiceMock.Setup(s => s.IsInRole("CenterManager")).Returns(true);

        _permissionRepoMock.Setup(p => p.GetActivePermissionAsync(senderOrgId, destOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrossCenterPermission?)null);

        var service = CreateService();
        var request = new CreateNotificationRequest
        {
            Title = "Cross Center",
            Message = "Hello Org 2",
            Type = MessageType.Information,
            Priority = MessagePriority.Normal,
            PublishStartAtUtc = DateTime.UtcNow,
            RecipientOrganizationId = destOrgId,
            RecipientUserIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateNotificationAsync(request));
        Assert.Contains("مجوز ارتباط بین مرکزها", ex.Message);
    }

    [Fact]
    public async Task CreateNotification_CenterManagerCrossCenter_WithActivePermission_Succeeds()
    {
        // Arrange
        var senderUserId = Guid.NewGuid();
        var senderOrgId = Guid.NewGuid();
        var destOrgId = Guid.NewGuid();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(senderUserId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(senderOrgId);
        _currentUserServiceMock.Setup(s => s.IsInRole("CenterManager")).Returns(true);

        _permissionRepoMock.Setup(p => p.GetActivePermissionAsync(senderOrgId, destOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrossCenterPermission
            {
                Id = Guid.NewGuid(),
                SourceOrganizationId = senderOrgId,
                DestinationOrganizationId = destOrgId,
                IsActive = true
            });

        var service = CreateService();
        var request = new CreateNotificationRequest
        {
            Title = "Cross Center Authorized",
            Message = "Hello Org 2",
            Type = MessageType.Information,
            Priority = MessagePriority.Normal,
            PublishStartAtUtc = DateTime.UtcNow,
            RecipientOrganizationId = destOrgId,
            RecipientUserIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var result = await service.CreateNotificationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cross Center Authorized", result.Title);
        _notificationRepoMock.Verify(n => n.AddAsync(It.IsAny<Domain.Entities.Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRepliesForThread_GroupMessage_IsolatesRecipientThreads()
    {
        // Arrange
        var senderUserId = Guid.NewGuid();
        var recipientAUserId = Guid.NewGuid();
        var recipientBUserId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        var notification = new Domain.Entities.Notification
        {
            Id = notificationId,
            SenderUserId = senderUserId,
            Title = "Group Announcement",
            Message = "Hello everyone",
            Recipients = new List<NotificationRecipient>
            {
                new NotificationRecipient { NotificationId = notificationId, RecipientUserId = recipientAUserId },
                new NotificationRecipient { NotificationId = notificationId, RecipientUserId = recipientBUserId }
            }
        };

        _notificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        // When Recipient A accesses thread, targetRecipientUserId is automatically overridden to Recipient A's ID
        _currentUserServiceMock.Setup(s => s.UserId).Returns(recipientAUserId);

        _notificationRepoMock.Setup(r => r.GetRepliesForThreadAsync(notificationId, recipientAUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationReply>
            {
                new NotificationReply { Id = Guid.NewGuid(), NotificationId = notificationId, RecipientUserId = recipientAUserId, SenderUserId = recipientAUserId, Content = "A's private reply" }
            });

        var service = CreateService();

        // Act - Recipient A passes Recipient B's ID attempting IDOR
        var replies = await service.GetRepliesForThreadAsync(notificationId, recipientBUserId);

        // Assert - Recipient A receives only Recipient A's thread replies
        Assert.Single(replies);
        Assert.Equal("A's private reply", replies[0].Content);
        _notificationRepoMock.Verify(r => r.GetRepliesForThreadAsync(notificationId, recipientAUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Acknowledge_WhenNotRequired_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _notificationRepoMock.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Domain.Entities.Notification
            {
                Id = notificationId,
                RequiresAcknowledgment = false
            });

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AcknowledgeAsync(notificationId));
        Assert.Contains("نیازمند تایید رسمی دریافت نیست", ex.Message);
    }

    [Fact]
    public async Task RegisterDevice_SavesOrUpdatesUserDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var token = "FCM_TOKEN_12345";

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _currentUserServiceMock.Setup(s => s.OrganizationId).Returns(orgId);

        _deviceRepoMock.Setup(d => d.GetByDeviceTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDevice?)null);

        var service = CreateService();
        var request = new RegisterDeviceRequest
        {
            DeviceToken = token,
            DeviceType = "Android"
        };

        // Act
        await service.RegisterDeviceAsync(request);

        // Assert
        _deviceRepoMock.Verify(d => d.AddAsync(It.Is<UserDevice>(ud => ud.DeviceToken == token && ud.UserId == userId && ud.OrganizationId == orgId), It.IsAny<CancellationToken>()), Times.Once);
    }
}
