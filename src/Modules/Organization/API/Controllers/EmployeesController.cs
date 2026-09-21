using System.Security.Claims;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateEmployee;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Organization.API.Controllers;

[ApiController]
[Route("api/organizations/employees")]
[Authorize(Roles = "CenterManager,SystemAdmin")]
public class EmployeesController : ControllerBase
{
    private readonly ICreateEmployeeUseCase _createEmployeeUseCase;
    private readonly ICurrentUserService _currentUserService;

    public EmployeesController(
        ICreateEmployeeUseCase createEmployeeUseCase,
        ICurrentUserService currentUserService)
    {
        _createEmployeeUseCase = createEmployeeUseCase ?? throw new ArgumentNullException(nameof(createEmployeeUseCase));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
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
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "سازمان مشخص نشده است",
                    Detail = "شناسه سازمان معتبر در درخواست ارسال نشده است."
                });
            }
        }
        else
        {
            var userOrgId = _currentUserService.OrganizationId;
            if (!userOrgId.HasValue || userOrgId.Value == Guid.Empty)
            {
                return Forbid();
            }

            targetOrganizationId = userOrgId.Value;
        }

        try
        {
            var result = await _createEmployeeUseCase.ExecuteAsync(
                request,
                targetOrganizationId,
                currentUserId,
                cancellationToken);

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
}
