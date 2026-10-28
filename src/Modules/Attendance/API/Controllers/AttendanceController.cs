using AriaHR.Modules.Attendance.Application.DTOs;
using AriaHR.Modules.Attendance.Application.Exceptions;
using AriaHR.Modules.Attendance.Application.UseCases;
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

    public AttendanceController(
        ICheckInUseCase checkInUseCase,
        ICheckOutUseCase checkOutUseCase,
        IGetMyAttendanceStatusUseCase getMyAttendanceStatusUseCase,
        IGetMyAttendanceHistoryUseCase getMyAttendanceHistoryUseCase,
        IGetOrganizationAttendanceUseCase getOrganizationAttendanceUseCase,
        IUpdateAttendanceStatusUseCase updateAttendanceStatusUseCase)
    {
        _checkInUseCase = checkInUseCase ?? throw new ArgumentNullException(nameof(checkInUseCase));
        _checkOutUseCase = checkOutUseCase ?? throw new ArgumentNullException(nameof(checkOutUseCase));
        _getMyAttendanceStatusUseCase = getMyAttendanceStatusUseCase ?? throw new ArgumentNullException(nameof(getMyAttendanceStatusUseCase));
        _getMyAttendanceHistoryUseCase = getMyAttendanceHistoryUseCase ?? throw new ArgumentNullException(nameof(getMyAttendanceHistoryUseCase));
        _getOrganizationAttendanceUseCase = getOrganizationAttendanceUseCase ?? throw new ArgumentNullException(nameof(getOrganizationAttendanceUseCase));
        _updateAttendanceStatusUseCase = updateAttendanceStatusUseCase ?? throw new ArgumentNullException(nameof(updateAttendanceStatusUseCase));
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

    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<AttendanceRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyHistory(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        var result = await _getMyAttendanceHistoryUseCase.ExecuteAsync(startDate, endDate, cancellationToken);
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
