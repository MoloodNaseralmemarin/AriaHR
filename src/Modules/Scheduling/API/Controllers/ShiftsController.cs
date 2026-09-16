using System.Security.Claims;
using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.UseCases.DefineShift;
using AriaHR.Modules.Scheduling.Application.UseCases.GetActiveShifts;
using AriaHR.Modules.Scheduling.Application.UseCases.GetShiftById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Scheduling.API.Controllers;

[ApiController]
[Route("api/scheduling/shifts")]
[Authorize(Roles = "CenterManager,SystemAdmin")]
public class ShiftsController : ControllerBase
{
    private readonly IDefineShiftUseCase _defineShiftUseCase;
    private readonly IGetActiveShiftsUseCase _getActiveShiftsUseCase;
    private readonly IGetShiftByIdUseCase _getShiftByIdUseCase;

    public ShiftsController(
        IDefineShiftUseCase defineShiftUseCase,
        IGetActiveShiftsUseCase getActiveShiftsUseCase,
        IGetShiftByIdUseCase getShiftByIdUseCase)
    {
        _defineShiftUseCase = defineShiftUseCase ?? throw new ArgumentNullException(nameof(defineShiftUseCase));
        _getActiveShiftsUseCase = getActiveShiftsUseCase ?? throw new ArgumentNullException(nameof(getActiveShiftsUseCase));
        _getShiftByIdUseCase = getShiftByIdUseCase ?? throw new ArgumentNullException(nameof(getShiftByIdUseCase));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ShiftDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DefineShift(
        [FromBody] DefineShiftRequest request,
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
            var result = await _defineShiftUseCase.ExecuteAsync(request, orgId, cancellationToken);
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

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ShiftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActiveShifts(CancellationToken cancellationToken)
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

        var result = await _getActiveShiftsUseCase.ExecuteAsync(orgId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShiftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetShiftById(Guid id, CancellationToken cancellationToken)
    {
        Guid orgId = GetOrganizationId(null);
        var result = await _getShiftByIdUseCase.ExecuteAsync(id, orgId, cancellationToken);
        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "شیفت یافت نشد",
                Detail = "شیفت مورد نظر با این مشخصات یافت نشد."
            });
        }

        return Ok(result);
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
