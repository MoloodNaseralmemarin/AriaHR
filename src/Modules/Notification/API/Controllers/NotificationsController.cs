using AriaHR.Modules.Notification.Application.DTOs;
using AriaHR.Modules.Notification.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Notification.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Creates and sends a new notification/message to authorized recipients.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SystemAdmin,CenterManager,Employee")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.CreateNotificationAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetNotificationById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "امکان ثبت پیام وجود ندارد",
                Detail = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "عدم دسترسی",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Retrieves active inbox notifications for the currently authenticated user.
    /// Ordered by priority descending (Urgent -> High -> Normal) and publication timestamp descending.
    /// Excludes future and expired messages.
    /// </summary>
    [HttpGet("inbox")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveInbox(CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetActiveInboxAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves historical notifications for the current user including past and expired messages.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetHistoryAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves notifications sent by the current user.
    /// </summary>
    [HttpGet("sent")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSent(CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetSentAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single notification by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetNotificationById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.GetByIdAsync(id, cancellationToken);
            if (result == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "پیام یافت نشد",
                    Detail = "پیام مورد نظر یافت نشد یا شما دسترسی به آن ندارید."
                });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "عدم دسترسی",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Marks a notification as read for the current recipient.
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.MarkAsReadAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "یافت نشد",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Explicitly acknowledges receipt of a notification requiring acknowledgment.
    /// </summary>
    [HttpPatch("{id:guid}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.AcknowledgeAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "یافت نشد",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "عملیات غیرمجاز",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Updates publication status (Draft, Published, Deactivated) for a message.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePublicationStatus(Guid id, [FromBody] UpdatePublicationStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.UpdatePublicationStatusAsync(id, request, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "پارامتر نامعتبر",
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "پیام یافت نشد",
                Detail = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "عدم دسترسی",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Submits a private reply for a specific recipient conversation thread.
    /// </summary>
    [HttpPost("{id:guid}/replies")]
    [ProducesResponseType(typeof(NotificationReplyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReply(Guid id, [FromBody] CreateReplyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.CreateReplyAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "پارامتر نامعتبر",
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "پیام یا گیرنده یافت نشد",
                Detail = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "عدم دسترسی",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Retrieves replies in a private conversation thread for a target recipient (ordered chronologically, oldest first).
    /// </summary>
    [HttpGet("{id:guid}/replies")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationReplyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRepliesForThread(Guid id, [FromQuery] Guid targetRecipientUserId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.GetRepliesForThreadAsync(id, targetRecipientUserId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "پیام یافت نشد",
                Detail = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "عدم دسترسی",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Retrieves delivery, read, and acknowledgment states of all recipients for a notification (Sender only).
    /// </summary>
    [HttpGet("{id:guid}/recipients")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationRecipientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecipientsForNotification(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.GetRecipientsForNotificationAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "پیام یافت نشد",
                Detail = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "عدم دسترسی",
                Detail = ex.Message
            });
        }
    }
}
