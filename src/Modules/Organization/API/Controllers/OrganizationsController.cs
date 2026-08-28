using System.Security.Claims;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
using AriaHR.Modules.Organization.Application.UseCases.GetTotalOrganizationsCount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Organization.API.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize(Roles = "SystemAdmin")]
public class OrganizationsController : ControllerBase
{
    private readonly ICreateOrganizationUseCase _createOrganizationUseCase;
    private readonly IGetTotalOrganizationsCountUseCase _getTotalOrganizationsCountUseCase;

    public OrganizationsController(
        ICreateOrganizationUseCase createOrganizationUseCase,
        IGetTotalOrganizationsCountUseCase getTotalOrganizationsCountUseCase)
    {
        _createOrganizationUseCase = createOrganizationUseCase ?? throw new ArgumentNullException(nameof(createOrganizationUseCase));
        _getTotalOrganizationsCountUseCase = getTotalOrganizationsCountUseCase ?? throw new ArgumentNullException(nameof(getTotalOrganizationsCountUseCase));
    }

    [HttpGet("count")]
    [ProducesResponseType(typeof(OrganizationCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCount(CancellationToken cancellationToken)
    {
        var result = await _getTotalOrganizationsCountUseCase.ExecuteAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

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
            var result = await _createOrganizationUseCase.ExecuteAsync(request, userId, cancellationToken);
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
    }
}
