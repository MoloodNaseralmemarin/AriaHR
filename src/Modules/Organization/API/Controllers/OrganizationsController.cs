using System.Security.Claims;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
using AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;
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

    public OrganizationsController(ICreateOrganizationUseCase createOrganizationUseCase)
    {
        _createOrganizationUseCase = createOrganizationUseCase ?? throw new ArgumentNullException(nameof(createOrganizationUseCase));
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _createOrganizationUseCase.ExecuteAsync(request, userId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("dashboard-summary")]
    [ProducesResponseType(typeof(OrganizationsDashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboardSummary(
        [FromServices] IGetOrganizationsDashboardSummaryUseCase getDashboardSummaryUseCase,
        CancellationToken cancellationToken)
    {
        var summary = await getDashboardSummaryUseCase.ExecuteAsync(cancellationToken);
        return Ok(summary);
    }
}
