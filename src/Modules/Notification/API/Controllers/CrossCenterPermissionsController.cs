using AriaHR.Modules.Notification.Application.DTOs;
using AriaHR.Modules.Notification.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Notification.API.Controllers;

[ApiController]
[Route("api/notifications/cross-center-permissions")]
[Authorize(Roles = "SystemAdmin")]
public class CrossCenterPermissionsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public CrossCenterPermissionsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Grants explicit permission for cross-center messaging from a source center to a destination center (SystemAdmin only).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CrossCenterPermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GrantPermission([FromBody] GrantCrossCenterPermissionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.GrantCrossCenterPermissionAsync(request, cancellationToken);
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
    /// Revokes an existing cross-center messaging permission (SystemAdmin only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokePermission(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.RevokeCrossCenterPermissionAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "مجوز یافت نشد",
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
    /// Lists all cross-center messaging permissions (SystemAdmin only).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CrossCenterPermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.GetCrossCenterPermissionsAsync(cancellationToken);
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
}
