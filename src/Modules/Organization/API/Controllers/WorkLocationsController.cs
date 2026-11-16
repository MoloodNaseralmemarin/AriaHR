using System.Security.Claims;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Exceptions;
using AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;
using AriaHR.Modules.Organization.Application.UseCases.GetWorkLocations;
using AriaHR.Modules.Organization.Application.UseCases.QrCode;
using AriaHR.Modules.Organization.Application.UseCases.UpdateWorkLocation;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Organization.API.Controllers;

[ApiController]
[Route("api/WorkLocations/work-locations")]
[Authorize(Roles = "CenterManager,SystemAdmin")]
public class WorkLocationsController : ControllerBase
{
    private readonly ICreateWorkLocationUseCase _createWorkLocationUseCase;
    private readonly IUpdateWorkLocationUseCase _updateWorkLocationUseCase;
    private readonly IGetWorkLocationsUseCase _getWorkLocationsUseCase;
    private readonly IQrCodeManagementUseCase _qrCodeManagementUseCase;
    private readonly ICurrentUserService _currentUserService;

    public WorkLocationsController(
        ICreateWorkLocationUseCase createWorkLocationUseCase,
        IUpdateWorkLocationUseCase updateWorkLocationUseCase,
        IGetWorkLocationsUseCase getWorkLocationsUseCase,
        IQrCodeManagementUseCase qrCodeManagementUseCase,
        ICurrentUserService currentUserService)
    {
        _createWorkLocationUseCase = createWorkLocationUseCase ?? throw new ArgumentNullException(nameof(createWorkLocationUseCase));
        _updateWorkLocationUseCase = updateWorkLocationUseCase ?? throw new ArgumentNullException(nameof(updateWorkLocationUseCase));
        _getWorkLocationsUseCase = getWorkLocationsUseCase ?? throw new ArgumentNullException(nameof(getWorkLocationsUseCase));
        _qrCodeManagementUseCase = qrCodeManagementUseCase ?? throw new ArgumentNullException(nameof(qrCodeManagementUseCase));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWorkLocations(
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        Guid organization;
        if (_currentUserService.IsInRole("SystemAdmin"))
        {
            if (!organizationId.HasValue || organizationId.Value == Guid.Empty)
            {
                return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "سازمان مشخص نشده است",
                    "برای مدیر سیستم، پارامتر organizationId الزامی است."));
            }

            organization = organizationId.Value;
        }
        else
        {
            if (organizationId.HasValue && organizationId.Value != Guid.Empty &&
                organizationId.Value != _currentUserService.OrganizationId)
            {
                return Forbid();
            }

            var scope = ResolveScope(out var scopeError);
            if (scopeError != null)
            {
                return scopeError;
            }

            organization = scope!.Value;
        }

        try
        {
            var result = await _getWorkLocationsUseCase.GetByOrganizationAsync(organization, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (ex is ArgumentException)
        {
            return ToErrorResult(ex);
        }
    }

    [HttpGet("{workLocationId:guid}")]
    [ProducesResponseType(typeof(WorkLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkLocation(
        [FromRoute] Guid workLocationId,
        CancellationToken cancellationToken)
    {
        var scope = ResolveScope(out var scopeError);
        if (scopeError != null)
        {
            return scopeError;
        }

        try
        {
            var result = await _getWorkLocationsUseCase.GetByIdAsync(workLocationId, scope, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ToErrorResult(ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkLocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateWorkLocation(
        [FromBody] CreateWorkLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "ورودی نامعتبر",
                "درخواست ارسال شده معتبر نمی‌باشد."));
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

        Guid targetOrganizationId;

        if (_currentUserService.IsInRole("SystemAdmin"))
        {
            if (request.OrganizationId.HasValue && request.OrganizationId.Value != Guid.Empty)
            {
                targetOrganizationId = request.OrganizationId.Value;
            }
            else if (_currentUserService.OrganizationId.HasValue && _currentUserService.OrganizationId.Value != Guid.Empty)
            {
                targetOrganizationId = _currentUserService.OrganizationId.Value;
            }
            else
            {
                return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "سازمان مشخص نشده است",
                    "شناسه سازمان معتبر در درخواست ارسال نشده است."));
            }
        }
        else
        {
            var userOrgId = _currentUserService.OrganizationId;
            if (!userOrgId.HasValue || userOrgId.Value == Guid.Empty)
            {
                return Forbid();
            }

            if (request.OrganizationId.HasValue && request.OrganizationId.Value != Guid.Empty &&
                request.OrganizationId.Value != userOrgId.Value)
            {
                return Forbid();
            }

            targetOrganizationId = userOrgId.Value;
        }

        try
        {
            var result = await _createWorkLocationUseCase.ExecuteAsync(
                request,
                targetOrganizationId,
                currentUserId,
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (Exception ex)
        {
            return ToErrorResult(ex);
        }
    }

    [HttpPut("{workLocationId:guid}")]
    [ProducesResponseType(typeof(WorkLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWorkLocation(
        [FromRoute] Guid workLocationId,
        [FromBody] UpdateWorkLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "ورودی نامعتبر",
                "درخواست ارسال شده معتبر نمی‌باشد."));
        }

        var scope = ResolveScope(out var scopeError);
        if (scopeError != null)
        {
            return scopeError;
        }

        try
        {
            var result = await _updateWorkLocationUseCase.ExecuteAsync(
                workLocationId,
                request,
                _currentUserService.UserId,
                scope,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ToErrorResult(ex);
        }
    }

    [HttpGet("{workLocationId:guid}/qrcode/settings")]
    [ProducesResponseType(typeof(QrCodeSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCodeSettings(
        [FromRoute] Guid workLocationId,
        CancellationToken cancellationToken)
    {
        var scope = ResolveScope(out var scopeError);
        if (scopeError != null)
        {
            return scopeError;
        }

        try
        {
            var result = await _qrCodeManagementUseCase.GetSettingsAsync(workLocationId, scope, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ToErrorResult(ex);
        }
    }

    [HttpPut("{workLocationId:guid}/qrcode/settings")]
    [ProducesResponseType(typeof(QrCodeSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQrCodeSettings(
        [FromRoute] Guid workLocationId,
        [FromBody] UpdateQrCodeSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "ورودی نامعتبر",
                "درخواست ارسال شده معتبر نمی‌باشد."));
        }

        var scope = ResolveScope(out var scopeError);
        if (scopeError != null)
        {
            return scopeError;
        }

        try
        {
            var result = await _qrCodeManagementUseCase.UpdateSettingsAsync(
                workLocationId,
                request,
                _currentUserService.UserId,
                scope,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ToErrorResult(ex);
        }
    }

    [HttpGet("{workLocationId:guid}/qrcode/current")]
    [ProducesResponseType(typeof(QrCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentQrCode(
        [FromRoute] Guid workLocationId,
        CancellationToken cancellationToken)
    {
        var scope = ResolveScope(out var scopeError);
        if (scopeError != null)
        {
            return scopeError;
        }

        try
        {
            var result = await _qrCodeManagementUseCase.GetCurrentAsync(workLocationId, scope, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ToErrorResult(ex);
        }
    }

    [HttpPost("{workLocationId:guid}/qrcode/generate")]
    [ProducesResponseType(typeof(QrCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateQrCode(
        [FromRoute] Guid workLocationId,
        CancellationToken cancellationToken)
    {
        var scope = ResolveScope(out var scopeError);
        if (scopeError != null)
        {
            return scopeError;
        }

        try
        {
            var result = await _qrCodeManagementUseCase.GenerateAsync(
                workLocationId,
                _currentUserService.UserId,
                scope,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ToErrorResult(ex);
        }
    }

    [HttpPost("{workLocationId:guid}/qr")]
    [ProducesResponseType(typeof(QrCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GenerateLegacyQrCode(
        [FromRoute] Guid workLocationId,
        CancellationToken cancellationToken)
    {
        return GenerateQrCode(workLocationId, cancellationToken);
    }

    [HttpPost("{workLocationId:guid}/qrcode/enable")]
    [ProducesResponseType(typeof(QrCodeSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnableQrCode(
        [FromRoute] Guid workLocationId,
        CancellationToken cancellationToken)
    {
        var scope = ResolveScope(out var scopeError);
        if (scopeError != null)
        {
            return scopeError;
        }

        try
        {
            var result = await _qrCodeManagementUseCase.SetEnabledAsync(
                workLocationId,
                true,
                _currentUserService.UserId,
                scope,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ToErrorResult(ex);
        }
    }

    [HttpPost("{workLocationId:guid}/qrcode/disable")]
    [ProducesResponseType(typeof(QrCodeSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisableQrCode(
        [FromRoute] Guid workLocationId,
        CancellationToken cancellationToken)
    {
        var scope = ResolveScope(out var scopeError);
        if (scopeError != null)
        {
            return scopeError;
        }

        try
        {
            var result = await _qrCodeManagementUseCase.SetEnabledAsync(
                workLocationId,
                false,
                _currentUserService.UserId,
                scope,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ToErrorResult(ex);
        }
    }

    [HttpPost("{workLocationId:guid}/qrcode/revoke")]
    [ProducesResponseType(typeof(QrCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeQrCode(
        [FromRoute] Guid workLocationId,
        CancellationToken cancellationToken)
    {
        var scope = ResolveScope(out var scopeError);
        if (scopeError != null)
        {
            return scopeError;
        }

        try
        {
            var result = await _qrCodeManagementUseCase.RevokeAsync(
                workLocationId,
                _currentUserService.UserId,
                scope,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ToErrorResult(ex);
        }
    }

    /// <summary>
    /// Resolves the center scope for the current manager. System administrators are unrestricted (null);
    /// center managers are always constrained to their own organization from the JWT.
    /// </summary>
    private Guid? ResolveScope(out IActionResult? errorResult)
    {
        errorResult = null;

        if (_currentUserService.IsInRole("SystemAdmin"))
        {
            return null;
        }

        var organizationId = _currentUserService.OrganizationId;
        if (!organizationId.HasValue || organizationId.Value == Guid.Empty)
        {
            errorResult = Forbid();
            return null;
        }

        return organizationId.Value;
    }

    private IActionResult ToErrorResult(Exception ex)
    {
        switch (ex)
        {
            case UnauthorizedAccessException:
                return Forbid();
            case KeyNotFoundException notFound:
                return NotFound(CreateProblem(StatusCodes.Status404NotFound, "یافت نشد", notFound.Message));
            case QrCodeOperationException qrError:
                return StatusCode(qrError.StatusCode, CreateProblem(qrError.StatusCode, "خطای کد کیوآر", qrError.Message, qrError.ErrorCode));
            case ArgumentException argument:
                return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "ورودی نامعتبر", argument.Message));
            case InvalidOperationException invalidOperation:
                return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "خطای کسب و کار", invalidOperation.Message));
            default:
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail, string? errorCode = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            problem.Extensions["errorCode"] = errorCode;
        }

        return problem;
    }
}
