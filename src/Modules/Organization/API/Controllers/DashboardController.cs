using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentActivities;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentCenters;
using AriaHR.Modules.Organization.Application.UseCases.GetSystemAdminDashboardOverview;
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
    private readonly IGetSystemAdminDashboardOverviewUseCase _getDashboardOverviewUseCase;
    private readonly IGetRecentCentersUseCase _getRecentCentersUseCase;

    public DashboardController(
        IGetRecentActivitiesUseCase getRecentActivitiesUseCase,
        IGetSystemAdminDashboardOverviewUseCase getDashboardOverviewUseCase,
        IGetRecentCentersUseCase getRecentCentersUseCase)
    {
        _getRecentActivitiesUseCase = getRecentActivitiesUseCase ?? throw new ArgumentNullException(nameof(getRecentActivitiesUseCase));
        _getDashboardOverviewUseCase = getDashboardOverviewUseCase ?? throw new ArgumentNullException(nameof(getDashboardOverviewUseCase));
        _getRecentCentersUseCase = getRecentCentersUseCase ?? throw new ArgumentNullException(nameof(getRecentCentersUseCase));
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(SystemAdminDashboardOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await _getDashboardOverviewUseCase.ExecuteAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("recent-centers")]
    [ProducesResponseType(typeof(PagedResult<RecentCenterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecentCenters(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await _getRecentCentersUseCase.ExecuteAsync(page, pageSize, cancellationToken);
        return Ok(result);
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
