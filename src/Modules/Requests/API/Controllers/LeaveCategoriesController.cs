using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.UseCases.ActivateLeaveCategory;
using AriaHR.Modules.Requests.Application.UseCases.CreateLeaveCategory;
using AriaHR.Modules.Requests.Application.UseCases.DeactivateLeaveCategory;
using AriaHR.Modules.Requests.Application.UseCases.GetLeaveCategories;
using AriaHR.Modules.Requests.Application.UseCases.UpdateLeaveCategory;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Requests.API.Controllers;

[ApiController]
[Route("api/requests/leave-categories")]
[Authorize(Roles = "CenterManager,SystemAdmin")]
public class LeaveCategoriesController : ControllerBase
{
    private readonly ICreateLeaveCategoryUseCase _createLeaveCategoryUseCase;
    private readonly IGetLeaveCategoriesUseCase _getLeaveCategoriesUseCase;
    private readonly IUpdateLeaveCategoryUseCase _updateLeaveCategoryUseCase;
    private readonly IActivateLeaveCategoryUseCase _activateLeaveCategoryUseCase;
    private readonly IDeactivateLeaveCategoryUseCase _deactivateLeaveCategoryUseCase;
    private readonly ICurrentUserService _currentUserService;

    public LeaveCategoriesController(
        ICreateLeaveCategoryUseCase createLeaveCategoryUseCase,
        IGetLeaveCategoriesUseCase getLeaveCategoriesUseCase,
        IUpdateLeaveCategoryUseCase updateLeaveCategoryUseCase,
        IActivateLeaveCategoryUseCase activateLeaveCategoryUseCase,
        IDeactivateLeaveCategoryUseCase deactivateLeaveCategoryUseCase,
        ICurrentUserService currentUserService)
    {
        _createLeaveCategoryUseCase = createLeaveCategoryUseCase ?? throw new ArgumentNullException(nameof(createLeaveCategoryUseCase));
        _getLeaveCategoriesUseCase = getLeaveCategoriesUseCase ?? throw new ArgumentNullException(nameof(getLeaveCategoriesUseCase));
        _updateLeaveCategoryUseCase = updateLeaveCategoryUseCase ?? throw new ArgumentNullException(nameof(updateLeaveCategoryUseCase));
        _activateLeaveCategoryUseCase = activateLeaveCategoryUseCase ?? throw new ArgumentNullException(nameof(activateLeaveCategoryUseCase));
        _deactivateLeaveCategoryUseCase = deactivateLeaveCategoryUseCase ?? throw new ArgumentNullException(nameof(deactivateLeaveCategoryUseCase));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [Authorize(Roles = "CenterManager,SystemAdmin,Employee")]
    [ProducesResponseType(typeof(IEnumerable<LeaveCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLeaveCategories(
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        Guid userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        Guid targetOrgId;

        if (!_currentUserService.IsInRole("SystemAdmin"))
        {
            var userOrgId = _currentUserService.OrganizationId;
            if (!userOrgId.HasValue || userOrgId.Value == Guid.Empty)
            {
                return Forbid();
            }

            if (organizationId.HasValue && organizationId.Value != Guid.Empty && organizationId.Value != userOrgId.Value)
            {
                return Forbid();
            }

            targetOrgId = userOrgId.Value;
        }
        else
        {
            targetOrgId = _currentUserService.ResolveOrganizationId(organizationId);
            if (targetOrgId == Guid.Empty)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "سازمان مشخص نشده است",
                    Detail = "شناسه سازمان معتبر در درخواست یا توکن یافت نشد."
                });
            }
        }

        try
        {
            var result = await _getLeaveCategoriesUseCase.ExecuteAsync(targetOrgId, cancellationToken);
            return Ok(result);
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

    [HttpPost]
    [ProducesResponseType(typeof(LeaveCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateLeaveCategory(
        [FromBody] CreateLeaveCategoryRequest request,
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

        Guid userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        Guid targetOrgId;

        if (!_currentUserService.IsInRole("SystemAdmin"))
        {
            var userOrgId = _currentUserService.OrganizationId;
            if (!userOrgId.HasValue || userOrgId.Value == Guid.Empty)
            {
                return Forbid();
            }

            if (request.OrganizationId.HasValue && request.OrganizationId.Value != Guid.Empty && request.OrganizationId.Value != userOrgId.Value)
            {
                return Forbid();
            }

            targetOrgId = userOrgId.Value;
        }
        else
        {
            targetOrgId = _currentUserService.ResolveOrganizationId(request.OrganizationId);
            if (targetOrgId == Guid.Empty)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "سازمان مشخص نشده است",
                    Detail = "شناسه سازمان معتبر در درخواست یا توکن یافت نشد."
                });
            }
        }

        try
        {
            var result = await _createLeaveCategoryUseCase.ExecuteAsync(request, targetOrgId, userId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
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

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LeaveCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLeaveCategory(
        [FromRoute] Guid id,
        [FromBody] UpdateLeaveCategoryRequest request,
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

        Guid userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        bool isSystemAdmin = _currentUserService.IsInRole("SystemAdmin");
        Guid userOrgId = _currentUserService.OrganizationId ?? Guid.Empty;

        try
        {
            var result = await _updateLeaveCategoryUseCase.ExecuteAsync(
                id,
                request,
                userId,
                userOrgId,
                isSystemAdmin,
                cancellationToken);

            return Ok(result);
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
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "یافت نشد",
                Detail = ex.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(LeaveCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateLeaveCategory(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        bool isSystemAdmin = _currentUserService.IsInRole("SystemAdmin");
        Guid userOrgId = _currentUserService.OrganizationId ?? Guid.Empty;

        try
        {
            var result = await _activateLeaveCategoryUseCase.ExecuteAsync(
                id,
                userId,
                userOrgId,
                isSystemAdmin,
                cancellationToken);

            return Ok(result);
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
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "یافت نشد",
                Detail = ex.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(LeaveCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateLeaveCategory(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        bool isSystemAdmin = _currentUserService.IsInRole("SystemAdmin");
        Guid userOrgId = _currentUserService.OrganizationId ?? Guid.Empty;

        try
        {
            var result = await _deactivateLeaveCategoryUseCase.ExecuteAsync(
                id,
                userId,
                userOrgId,
                isSystemAdmin,
                cancellationToken);

            return Ok(result);
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
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "یافت نشد",
                Detail = ex.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
