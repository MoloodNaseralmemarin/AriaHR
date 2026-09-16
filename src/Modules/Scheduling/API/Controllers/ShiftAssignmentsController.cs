using System.Security.Claims;
using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.UseCases.AssignShift;
using AriaHR.Modules.Scheduling.Application.UseCases.GetEmployeeShiftCalendar;
using AriaHR.Modules.Scheduling.Application.UseCases.GetShiftCalendar;
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

    public ShiftAssignmentsController(
        IAssignShiftUseCase assignShiftUseCase,
        IGetShiftCalendarUseCase getShiftCalendarUseCase,
        IGetEmployeeShiftCalendarUseCase getEmployeeShiftCalendarUseCase)
    {
        _assignShiftUseCase = assignShiftUseCase ?? throw new ArgumentNullException(nameof(assignShiftUseCase));
        _getShiftCalendarUseCase = getShiftCalendarUseCase ?? throw new ArgumentNullException(nameof(getShiftCalendarUseCase));
        _getEmployeeShiftCalendarUseCase = getEmployeeShiftCalendarUseCase ?? throw new ArgumentNullException(nameof(getEmployeeShiftCalendarUseCase));
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

        Guid userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        Guid orgId = GetOrganizationId(request.OrganizationId);
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
        CancellationToken cancellationToken)
    {
        Guid orgId = GetOrganizationId(null);
        if (orgId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "سازمان مشخص نشده است",
                Detail = "شناسه سازمان معتبر در توکن یافت نشد."
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
        try
        {
            var result = await _getEmployeeShiftCalendarUseCase.ExecuteAsync(employeeId, startDate, endDate, cancellationToken);
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

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }

    private Guid GetOrganizationId(Guid? requestOrgId)
    {
        var orgClaim = User.FindFirstValue("organization_id");
        if (!string.IsNullOrEmpty(orgClaim) && Guid.TryParse(orgClaim, out var claimOrgId))
        {
            return claimOrgId;
        }

        if (User.IsInRole("SystemAdmin") && requestOrgId.HasValue && requestOrgId.Value != Guid.Empty)
        {
            return requestOrgId.Value;
        }

        return Guid.Empty;
    }
}
