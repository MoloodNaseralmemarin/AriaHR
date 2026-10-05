using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.UseCases.ApproveLeaveRequest;
using AriaHR.Modules.Requests.Application.UseCases.CancelLeaveRequest;
using AriaHR.Modules.Requests.Application.UseCases.CreateLeaveRequest;
using AriaHR.Modules.Requests.Application.UseCases.GetLeaveRequestById;
using AriaHR.Modules.Requests.Application.UseCases.GetMyLeaveRequests;
using AriaHR.Modules.Requests.Application.UseCases.GetOrganizationLeaveRequests;
using AriaHR.Modules.Requests.Application.UseCases.RejectLeaveRequest;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Requests.API.Controllers;

[ApiController]
[Route("api/requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ICreateLeaveRequestUseCase _createLeaveRequestUseCase;
    private readonly IGetMyLeaveRequestsUseCase _getMyLeaveRequestsUseCase;
    private readonly IGetLeaveRequestByIdUseCase _getLeaveRequestByIdUseCase;
    private readonly ICancelLeaveRequestUseCase _cancelLeaveRequestUseCase;
    private readonly IGetOrganizationLeaveRequestsUseCase _getOrganizationLeaveRequestsUseCase;
    private readonly IApproveLeaveRequestUseCase _approveLeaveRequestUseCase;
    private readonly IRejectLeaveRequestUseCase _rejectLeaveRequestUseCase;
    private readonly ICurrentUserService _currentUserService;

    public LeaveRequestsController(
        ICreateLeaveRequestUseCase createLeaveRequestUseCase,
        IGetMyLeaveRequestsUseCase getMyLeaveRequestsUseCase,
        IGetLeaveRequestByIdUseCase getLeaveRequestByIdUseCase,
        ICancelLeaveRequestUseCase cancelLeaveRequestUseCase,
        IGetOrganizationLeaveRequestsUseCase getOrganizationLeaveRequestsUseCase,
        IApproveLeaveRequestUseCase approveLeaveRequestUseCase,
        IRejectLeaveRequestUseCase rejectLeaveRequestUseCase,
        ICurrentUserService currentUserService)
    {
        _createLeaveRequestUseCase = createLeaveRequestUseCase ?? throw new ArgumentNullException(nameof(createLeaveRequestUseCase));
        _getMyLeaveRequestsUseCase = getMyLeaveRequestsUseCase ?? throw new ArgumentNullException(nameof(getMyLeaveRequestsUseCase));
        _getLeaveRequestByIdUseCase = getLeaveRequestByIdUseCase ?? throw new ArgumentNullException(nameof(getLeaveRequestByIdUseCase));
        _cancelLeaveRequestUseCase = cancelLeaveRequestUseCase ?? throw new ArgumentNullException(nameof(cancelLeaveRequestUseCase));
        _getOrganizationLeaveRequestsUseCase = getOrganizationLeaveRequestsUseCase ?? throw new ArgumentNullException(nameof(getOrganizationLeaveRequestsUseCase));
        _approveLeaveRequestUseCase = approveLeaveRequestUseCase ?? throw new ArgumentNullException(nameof(approveLeaveRequestUseCase));
        _rejectLeaveRequestUseCase = rejectLeaveRequestUseCase ?? throw new ArgumentNullException(nameof(rejectLeaveRequestUseCase));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpPost("leave")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateLeaveRequest(
        [FromBody] CreateLeaveRequestRequest request,
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

        try
        {
            var result = await _createLeaveRequestUseCase.ExecuteAsync(request, userId, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return Forbid();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(IEnumerable<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRequests(CancellationToken cancellationToken)
    {
        Guid userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var result = await _getMyLeaveRequestsUseCase.ExecuteAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequestById(
        Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        bool isSystemAdmin = _currentUserService.IsInRole("SystemAdmin");
        bool isCenterManager = _currentUserService.IsInRole("CenterManager");
        Guid? userOrgId = _currentUserService.OrganizationId;

        try
        {
            var result = await _getLeaveRequestByIdUseCase.ExecuteAsync(
                id,
                userId,
                isSystemAdmin,
                isCenterManager,
                userOrgId,
                cancellationToken);

            if (result == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "یافت نشد",
                    Detail = "درخواست مورد نظر یافت نشد."
                });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        try
        {
            await _cancelLeaveRequestUseCase.ExecuteAsync(id, userId, cancellationToken);
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "خطای عملیات",
                Detail = ex.Message
            });
        }
    }

    [HttpGet]
    [Authorize(Roles = "CenterManager,SystemAdmin")]
    [ProducesResponseType(typeof(IEnumerable<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrganizationRequests(
        [FromQuery] GetOrganizationRequestsQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsInRole("SystemAdmin"))
        {
            var userOrgId = _currentUserService.OrganizationId;
            if (!userOrgId.HasValue || userOrgId.Value == Guid.Empty)
            {
                return Forbid();
            }
        }

        Guid orgId = _currentUserService.ResolveOrganizationId();
        if (orgId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "سازمان مشخص نشده است",
                Detail = "شناسه سازمان معتبر در توکن یا درخواست یافت نشد."
            });
        }

        try
        {
            var result = await _getOrganizationLeaveRequestsUseCase.ExecuteAsync(orgId, query, cancellationToken);
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

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "CenterManager,SystemAdmin")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveRequest(
        Guid id,
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
            var result = await _approveLeaveRequestUseCase.ExecuteAsync(id, userId, userOrgId, isSystemAdmin, cancellationToken);
            return Ok(result);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "خطای عملیات",
                Detail = ex.Message
            });
        }
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "CenterManager,SystemAdmin")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectRequest(
        Guid id,
        [FromBody] RejectLeaveRequestRequest request,
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
            var result = await _rejectLeaveRequestUseCase.ExecuteAsync(id, request, userId, userOrgId, isSystemAdmin, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "خطای عملیات",
                Detail = ex.Message
            });
        }
    }
}
