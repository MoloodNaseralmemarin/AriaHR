using System.Security.Claims;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.UpdateCenter;
using AriaHR.Modules.Organization.Application.UseCases.UpdateCenterStatus;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Organization.API.Controllers;

[ApiController]
[Route("api/centers")]
[Authorize(Roles = "CenterManager,SystemAdmin")]
public class CentersController : ControllerBase
{
    private readonly IUpdateCenterUseCase _updateCenterUseCase;
    private readonly IUpdateCenterStatusUseCase _updateCenterStatusUseCase;
    private readonly ICurrentUserService _currentUserService;

    public CentersController(
        IUpdateCenterUseCase updateCenterUseCase,
        IUpdateCenterStatusUseCase updateCenterStatusUseCase,
        ICurrentUserService currentUserService)
    {
        _updateCenterUseCase = updateCenterUseCase ?? throw new ArgumentNullException(nameof(updateCenterUseCase));
        _updateCenterStatusUseCase = updateCenterStatusUseCase ?? throw new ArgumentNullException(nameof(updateCenterStatusUseCase));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpPut("{centerId:guid}")]
    [ProducesResponseType(typeof(BranchListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCenter(
        [FromRoute] Guid centerId,
        [FromBody] UpdateBranchRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "ورودی نامعتبر",
                Detail = "درخواست ارسال شده معتبر نمی‌باشد."
            });
        }

        Guid currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out currentUserId))
            {
                return Unauthorized();
            }
        }

        bool isSystemAdmin = _currentUserService.IsInRole("SystemAdmin");
        Guid? userOrgId = _currentUserService.OrganizationId;

        try
        {
            var result = await _updateCenterUseCase.ExecuteAsync(
                centerId,
                request,
                currentUserId,
                isSystemAdmin,
                userOrgId,
                cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "ورودی نامعتبر",
                Detail = ex.Message
            });
        }
    }

    [HttpPatch("{centerId:guid}/status")]
    [ProducesResponseType(typeof(BranchListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCenterStatus(
        [FromRoute] Guid centerId,
        [FromBody] CenterStatusUpdateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "ورودی نامعتبر",
                Detail = "درخواست ارسال شده معتبر نمی‌باشد."
            });
        }

        Guid currentUserId = _currentUserService.UserId;
        if (currentUserId == Guid.Empty)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out currentUserId))
            {
                return Unauthorized();
            }
        }

        bool isSystemAdmin = _currentUserService.IsInRole("SystemAdmin");
        Guid? userOrgId = _currentUserService.OrganizationId;

        try
        {
            var result = await _updateCenterStatusUseCase.ExecuteAsync(
                centerId,
                request,
                currentUserId,
                isSystemAdmin,
                userOrgId,
                cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "ورودی نامعتبر",
                Detail = ex.Message
            });
        }
    }
}
