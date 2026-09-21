using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.UseCases.AssignShift;
using AriaHR.Modules.Scheduling.Application.UseCases.GetEmployeeShiftCalendar;
using AriaHR.Modules.Scheduling.Application.UseCases.GetShiftCalendar;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Scheduling.API.Controllers;

[ApiController]
[Route("api/scheduling/shift-assignments")]
[Authorize]
public class ShiftAssignmentsController : ControllerBase
{
    private readonly IAssignShiftUseCase _assignShiftUseCase;
    private readonly IGetShiftCalendarUseCase _getShiftCalendarUseCase;
    private readonly IGetEmployeeShiftCalendarUseCase _getEmployeeShiftCalendarUseCase;
    private readonly ICurrentUserService _currentUserService;

    public ShiftAssignmentsController(
        IAssignShiftUseCase assignShiftUseCase,
        IGetShiftCalendarUseCase getShiftCalendarUseCase,
        IGetEmployeeShiftCalendarUseCase getEmployeeShiftCalendarUseCase,
        ICurrentUserService currentUserService)
    {
        _assignShiftUseCase = assignShiftUseCase ?? throw new ArgumentNullException(nameof(assignShiftUseCase));
        _getShiftCalendarUseCase = getShiftCalendarUseCase ?? throw new ArgumentNullException(nameof(getShiftCalendarUseCase));
        _getEmployeeShiftCalendarUseCase = getEmployeeShiftCalendarUseCase ?? throw new ArgumentNullException(nameof(getEmployeeShiftCalendarUseCase));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpPost]
    [Authorize(Roles = "CenterManager,SystemAdmin")]
    [ProducesResponseType(typeof(ShiftAssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignShift(
        [FromBody] AssignShiftRequest request,
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
        }

        Guid orgId = _currentUserService.ResolveOrganizationId(request.OrganizationId);
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
            var result = await _assignShiftUseCase.ExecuteAsync(request, orgId, userId, cancellationToken);
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
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "خطای کسب و کار",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("calendar")]
    [ProducesResponseType(typeof(IEnumerable<ShiftAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCalendar(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
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
        }

        Guid orgId = _currentUserService.ResolveOrganizationId(organizationId);
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
            var result = await _getShiftCalendarUseCase.ExecuteAsync(orgId, startDate, endDate, cancellationToken);
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

    [HttpGet("employee/{employeeId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ShiftAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEmployeeCalendar(
        Guid employeeId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsInRole("SystemAdmin"))
        {
            var userOrg = _currentUserService.OrganizationId;
            if (!userOrg.HasValue || userOrg.Value == Guid.Empty)
            {
                return Forbid();
            }
        }

        Guid userOrgId = _currentUserService.IsInRole("SystemAdmin") ? Guid.Empty : (_currentUserService.OrganizationId ?? Guid.Empty);

        try
        {
            var result = await _getEmployeeShiftCalendarUseCase.ExecuteAsync(employeeId, startDate, endDate, userOrgId, cancellationToken);
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
}
