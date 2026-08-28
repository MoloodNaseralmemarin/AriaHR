using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentActivities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Organization.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "SystemAdmin")]
public class DashboardController : ControllerBase
{
    private readonly IGetRecentActivitiesUseCase _getRecentActivitiesUseCase;

    public DashboardController(IGetRecentActivitiesUseCase getRecentActivitiesUseCase)
    {
        _getRecentActivitiesUseCase = getRecentActivitiesUseCase ?? throw new ArgumentNullException(nameof(getRecentActivitiesUseCase));
    }

    [HttpGet("recent-activities")]
    [ProducesResponseType(typeof(IEnumerable<RecentActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecentActivities(CancellationToken cancellationToken)
    {
        var result = await _getRecentActivitiesUseCase.ExecuteAsync(cancellationToken);
        return Ok(result);
    }
}
