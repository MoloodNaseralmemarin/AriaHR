using AriaHR.Modules.Notification.Application.DTOs;
using AriaHR.Modules.Notification.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Notification.API.Controllers;

[ApiController]
[Route("api/notifications/devices")]
[Authorize]
public class UserDevicesController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public UserDevicesController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Registers or updates a push notification device token for the current user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.RegisterDeviceAsync(request, cancellationToken);
            return Ok(new { message = "توکن دستگاه با موفقیت ثبت شد." });
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
    }

    /// <summary>
    /// Deactivates a push notification device token for the current user.
    /// </summary>
    [HttpDelete("{deviceToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnregisterDevice(string deviceToken, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.UnregisterDeviceAsync(deviceToken, cancellationToken);
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
    }
}
