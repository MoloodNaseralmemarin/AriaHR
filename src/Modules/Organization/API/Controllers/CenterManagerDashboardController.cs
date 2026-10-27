using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.GetCenterManagerDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Organization.API.Controllers;

[ApiController]
[Route("api/center-manager/dashboard")]
[Authorize(Roles = "CenterManager")]
public class CenterManagerDashboardController : ControllerBase
{
    private readonly ICenterManagerDashboardUseCase _dashboardUseCase;

    public CenterManagerDashboardController(ICenterManagerDashboardUseCase dashboardUseCase)
    {
        _dashboardUseCase = dashboardUseCase ?? throw new ArgumentNullException(nameof(dashboardUseCase));
    }

    [HttpGet]
    [ProducesResponseType(typeof(CenterManagerDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dashboardUseCase.ExecuteAsync(cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = ex.Message
            });
        }
    }
}
