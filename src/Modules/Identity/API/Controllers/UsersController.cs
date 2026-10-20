using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Identity.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "SystemAdmin")]
public class UsersController : ControllerBase
{
    private readonly IGetCenterManagersUseCase _getCenterManagersUseCase;

    public UsersController(IGetCenterManagersUseCase getCenterManagersUseCase)
    {
        _getCenterManagersUseCase = getCenterManagersUseCase ?? throw new ArgumentNullException(nameof(getCenterManagersUseCase));
    }

    [HttpGet("center-managers")]
    [ProducesResponseType(typeof(IEnumerable<CenterManagerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCenterManagers(
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var centerManagers = await _getCenterManagersUseCase.ExecuteAsync(organizationId, cancellationToken);
        return Ok(centerManagers);
    }
}
