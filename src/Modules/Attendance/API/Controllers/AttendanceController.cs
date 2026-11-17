using AriaHR.Modules.Attendance.Application;
using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.Exceptions;
using AriaHR.Modules.Attendance.Application.UseCases;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Attendance.API.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly ICheckInUseCase _checkInUseCase;
    private readonly ICheckOutUseCase _checkOutUseCase;
    private readonly IGetMyAttendanceStatusUseCase _getMyAttendanceStatusUseCase;
    private readonly IGetMyAttendanceHistoryUseCase _getMyAttendanceHistoryUseCase;
    private readonly IGetOrganizationAttendanceUseCase _getOrganizationAttendanceUseCase;
    private readonly IUpdateAttendanceStatusUseCase _updateAttendanceStatusUseCase;
    private readonly IScanAttendanceUseCase _scanAttendanceUseCase;
    private readonly IEmployeeLookupService _employeeLookupService;
    private readonly IWorkLocationLookupService _workLocationLookupService;

    public AttendanceController(
        ICheckInUseCase checkInUseCase,
        ICheckOutUseCase checkOutUseCase,
        IGetMyAttendanceStatusUseCase getMyAttendanceStatusUseCase,
        IGetMyAttendanceHistoryUseCase getMyAttendanceHistoryUseCase,
        IGetOrganizationAttendanceUseCase getOrganizationAttendanceUseCase,
        IUpdateAttendanceStatusUseCase updateAttendanceStatusUseCase,
        IScanAttendanceUseCase scanAttendanceUseCase,
        IEmployeeLookupService employeeLookupService,
        IWorkLocationLookupService workLocationLookupService)
    {
        _checkInUseCase = checkInUseCase ?? throw new ArgumentNullException(nameof(checkInUseCase));
        _checkOutUseCase = checkOutUseCase ?? throw new ArgumentNullException(nameof(checkOutUseCase));
        _getMyAttendanceStatusUseCase = getMyAttendanceStatusUseCase ?? throw new ArgumentNullException(nameof(getMyAttendanceStatusUseCase));
        _getMyAttendanceHistoryUseCase = getMyAttendanceHistoryUseCase ?? throw new ArgumentNullException(nameof(getMyAttendanceHistoryUseCase));
        _getOrganizationAttendanceUseCase = getOrganizationAttendanceUseCase ?? throw new ArgumentNullException(nameof(getOrganizationAttendanceUseCase));
        _updateAttendanceStatusUseCase = updateAttendanceStatusUseCase ?? throw new ArgumentNullException(nameof(updateAttendanceStatusUseCase));
        _scanAttendanceUseCase = scanAttendanceUseCase ?? throw new ArgumentNullException(nameof(scanAttendanceUseCase));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
        _workLocationLookupService = workLocationLookupService ?? throw new ArgumentNullException(nameof(workLocationLookupService));
    }

    [HttpPost("scan")]
    [ProducesResponseType(typeof(AttendanceScanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AttendanceScanResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AttendanceScanResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(AttendanceScanResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(AttendanceScanResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(AttendanceScanResponse), StatusCodes.Status409Conflict)]
    /// <summary>
    /// Registers check-in or check-out from a work location QR code plus the employee's GPS reading.
    /// Employee identity is resolved from JWT claims; coordinates, radius and shift rules are validated server-side.
    /// </summary>
    public async Task<IActionResult> Scan(
        [FromBody] ScanAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(AttendanceScanResponse.Fail(
                AttendanceErrorCodes.InvalidRequest,
                "درخواست ارسال شده معتبر نمی‌باشد."));
        }

        try
        {
            var result = await _scanAttendanceUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (AttendanceScanException ex)
        {
            return StatusCode(ex.StatusCode, AttendanceScanResponse.Fail(ex.Code, ex.Message, ex.Data));
        }
        catch (LeaveRestrictionException ex)
        {
            return BadRequest(AttendanceScanResponse.Fail(ex.ErrorCode, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(AttendanceScanResponse.Fail(AttendanceErrorCodes.EmployeeNotAuthorized, ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(AttendanceScanResponse.Fail(AttendanceErrorCodes.InvalidRequest, ex.Message));
        }
    }

    [HttpPost("check-in")]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
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

        try
        {
            var result = await _checkInUseCase.ExecuteAsync(request, cancellationToken);
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
        catch (LeaveRestrictionException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "خطای مرخصی",
                Detail = ex.Message,
                Extensions = { ["errorCode"] = ex.ErrorCode }
            });
        }
        catch (InvalidOperationException ex)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "خطای کسب و کار",
                Detail = ex.Message
            };

            if (ex.Message.Contains("مرخصی"))
            {
                problemDetails.Extensions["errorCode"] = LeaveRestrictionException.DefaultErrorCode;
            }

            return BadRequest(problemDetails);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "دسترسی غیرمجاز",
                Detail = ex.Message
            });
        }
    }

    [HttpPost("check-out")]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckOut(
        [FromBody] CheckOutRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            request = new CheckOutRequest();
        }

        try
        {
            var result = await _checkOutUseCase.ExecuteAsync(request, cancellationToken);
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
        catch (LeaveRestrictionException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "خطای مرخصی",
                Detail = ex.Message,
                Extensions = { ["errorCode"] = ex.ErrorCode }
            });
        }
        catch (InvalidOperationException ex)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "خطای کسب و کار",
                Detail = ex.Message
            };

            if (ex.Message.Contains("مرخصی"))
            {
                problemDetails.Extensions["errorCode"] = LeaveRestrictionException.DefaultErrorCode;
            }

            return BadRequest(problemDetails);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "دسترسی غیرمجاز",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentStatus(CancellationToken cancellationToken)
    {
        var result = await _getMyAttendanceStatusUseCase.ExecuteAsync(cancellationToken);
        if (result == null)
        {
            return NoContent();
        }

        return Ok(result);
    }

    [HttpGet("worklocation")]
    [ProducesResponseType(typeof(WorkLocationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyWorkLocation(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == Guid.Empty)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "دسترسی غیرمجاز",
                Detail = "کاربر احراز هویت نشده است."
            });
        }

        var employeeDetails = await _employeeLookupService.GetEmployeeDetailsByUserIdAsync(userId, cancellationToken);
        if (!employeeDetails.HasValue)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "کارمند یافت نشد",
                Detail = "اطلاعات کارمندی برای کاربر جاری یافت نشد."
            });
        }

        var (employeeId, organizationId) = employeeDetails.Value;

        var workLocation = await _workLocationLookupService.GetWorkLocationForOrganizationAsync(organizationId, cancellationToken);

        if (workLocation == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "محل کار یافت نشد",
                Detail = "محل کار سازمانی یافت نشد."
            });
        }

        if (!workLocation.IsActive)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "محل کار غیرفعال",
                Detail = "محل کار شما حالياً غیرفعال است."
            };
        }

        return Ok(workLocation);
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResult<AttendanceRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = new MyAttendanceHistoryQuery
        {
            Page = page,
            PageSize = pageSize
        };

        var result = await _getMyAttendanceHistoryUseCase.ExecuteAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "CenterManager,SystemAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<AttendanceRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrganizationAttendance(
        [FromQuery] Guid? organizationId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] Guid? employeeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getOrganizationAttendanceUseCase.ExecuteAsync(
                organizationId, startDate, endDate, employeeId, cancellationToken);
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "دسترسی غیرمجاز",
                Detail = ex.Message
            });
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "CenterManager,SystemAdmin")]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateAttendanceStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || request == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "ورودی نامعتبر",
                Detail = "شناسه و اطلاعات ویرایش وضعیت الزامی است."
            });
        }

        try
        {
            var result = await _updateAttendanceStatusUseCase.ExecuteAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "رکورد یافت نشد",
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "دسترسی غیرمجاز",
                Detail = ex.Message
            });
        }
    }
}
