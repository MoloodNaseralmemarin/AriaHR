using AriaHR.Modules.Requests.Application.DTOs;
using AriaHR.Modules.Requests.Application.UseCases.ChangeLeaveTypeStatus;
using AriaHR.Modules.Requests.Application.UseCases.CreateLeaveType;
using AriaHR.Modules.Requests.Application.UseCases.GetLeaveTypeById;
using AriaHR.Modules.Requests.Application.UseCases.GetOrganizationLeaveTypes;
using AriaHR.Modules.Requests.Application.UseCases.UpdateLeaveType;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Requests.API.Controllers;

[ApiController]
[Route("api/leave-types")]
[Authorize(Roles = "CenterManager,SystemAdmin")]
public class LeaveTypesController : ControllerBase
{
    private readonly ICreateLeaveTypeUseCase _createLeaveTypeUseCase;
    private readonly IUpdateLeaveTypeUseCase _updateLeaveTypeUseCase;
    private readonly IChangeLeaveTypeStatusUseCase _changeLeaveTypeStatusUseCase;
    private readonly IGetLeaveTypeByIdUseCase _getLeaveTypeByIdUseCase;
    private readonly IGetOrganizationLeaveTypesUseCase _getOrganizationLeaveTypesUseCase;
    private readonly ICurrentUserService _currentUserService;

    public LeaveTypesController(
        ICreateLeaveTypeUseCase createLeaveTypeUseCase,
        IUpdateLeaveTypeUseCase updateLeaveTypeUseCase,
        IChangeLeaveTypeStatusUseCase changeLeaveTypeStatusUseCase,
        IGetLeaveTypeByIdUseCase getLeaveTypeByIdUseCase,
        IGetOrganizationLeaveTypesUseCase getOrganizationLeaveTypesUseCase,
        ICurrentUserService currentUserService)
    {
        _createLeaveTypeUseCase = createLeaveTypeUseCase ?? throw new ArgumentNullException(nameof(createLeaveTypeUseCase));
        _updateLeaveTypeUseCase = updateLeaveTypeUseCase ?? throw new ArgumentNullException(nameof(updateLeaveTypeUseCase));
        _changeLeaveTypeStatusUseCase = changeLeaveTypeStatusUseCase ?? throw new ArgumentNullException(nameof(changeLeaveTypeStatusUseCase));
        _getLeaveTypeByIdUseCase = getLeaveTypeByIdUseCase ?? throw new ArgumentNullException(nameof(getLeaveTypeByIdUseCase));
        _getOrganizationLeaveTypesUseCase = getOrganizationLeaveTypesUseCase ?? throw new ArgumentNullException(nameof(getOrganizationLeaveTypesUseCase));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LeaveTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLeaveTypes(
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

        var result = await _getOrganizationLeaveTypesUseCase.ExecuteAsync(orgId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeaveTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaveTypeById(
        Guid id,
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
            return Forbid();
        }

        var result = await _getLeaveTypeByIdUseCase.ExecuteAsync(id, orgId, cancellationToken);
        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "یافت نشد",
                Detail = "نوع مرخصی مورد نظر یافت نشد."
            });
        }

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LeaveTypeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateLeaveType(
        [FromBody] CreateLeaveTypeRequest request,
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

        Guid userId = _currentUserService.UserId;

        try
        {
            var result = await _createLeaveTypeUseCase.ExecuteAsync(request, orgId, userId, cancellationToken);
            return CreatedAtAction(nameof(GetLeaveTypeById), new { id = result.Id }, result);
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
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "نام تکراری",
                Detail = ex.Message
            });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LeaveTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLeaveType(
        Guid id,
        [FromBody] UpdateLeaveTypeRequest request,
        [FromQuery] Guid? organizationId,
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
            return Forbid();
        }

        Guid userId = _currentUserService.UserId;

        try
        {
            var result = await _updateLeaveTypeUseCase.ExecuteAsync(id, request, orgId, userId, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "نام تکراری",
                Detail = ex.Message
            });
        }
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(LeaveTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeLeaveTypeStatus(
        Guid id,
        [FromBody] ChangeLeaveTypeStatusRequest request,
        [FromQuery] Guid? organizationId,
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
            return Forbid();
        }

        Guid userId = _currentUserService.UserId;

        try
        {
            var result = await _changeLeaveTypeStatusUseCase.ExecuteAsync(id, request, orgId, userId, cancellationToken);
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
    }
}
