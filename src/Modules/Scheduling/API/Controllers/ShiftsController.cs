using AriaHR.Modules.Scheduling.Application.DTOs;
using AriaHR.Modules.Scheduling.Application.UseCases.DefineShift;
using AriaHR.Modules.Scheduling.Application.UseCases.GetActiveShifts;
using AriaHR.Modules.Scheduling.Application.UseCases.GetShiftById;
using AriaHR.Shared.Services;
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
    private readonly ICurrentUserService _currentUserService;

    public ShiftsController(
        IDefineShiftUseCase defineShiftUseCase,
        IGetActiveShiftsUseCase getActiveShiftsUseCase,
        IGetShiftByIdUseCase getShiftByIdUseCase,
        ICurrentUserService currentUserService)
    {
        _defineShiftUseCase = defineShiftUseCase ?? throw new ArgumentNullException(nameof(defineShiftUseCase));
        _getActiveShiftsUseCase = getActiveShiftsUseCase ?? throw new ArgumentNullException(nameof(getActiveShiftsUseCase));
        _getShiftByIdUseCase = getShiftByIdUseCase ?? throw new ArgumentNullException(nameof(getShiftByIdUseCase));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
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
    public async Task<IActionResult> GetActiveShifts(
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
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
        Guid orgId = _currentUserService.ResolveOrganizationId(null);
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
}
