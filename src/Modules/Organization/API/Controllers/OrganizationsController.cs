using System.Security.Claims;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateOrganization;
using AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;
using AriaHR.Modules.Organization.Application.UseCases.GetOrganizationCenters;
using AriaHR.Modules.Organization.Application.UseCases.GetOrganizations;
using AriaHR.Modules.Organization.Application.UseCases.GetRecentOrganizations;
using AriaHR.Modules.Organization.Application.UseCases.GetTotalOrganizationsCount;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Organization.API.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize(Roles = "CenterManager,SystemAdmin")]
public class OrganizationsController : ControllerBase
{
    private readonly ICreateOrganizationUseCase _createOrganizationUseCase;
    private readonly IGetTotalOrganizationsCountUseCase _getTotalOrganizationsCountUseCase;
    private readonly IGetOrganizationsDashboardSummaryUseCase _getOrganizationsDashboardSummaryUseCase;
    private readonly IGetRecentOrganizationsUseCase _getRecentOrganizationsUseCase;
    private readonly IGetOrganizationsUseCase? _getOrganizationsUseCase;
    private readonly IGetOrganizationCentersUseCase? _getOrganizationCentersUseCase;
    private readonly ICurrentUserService? _currentUserService;

    public OrganizationsController(
        ICreateOrganizationUseCase createOrganizationUseCase,
        IGetTotalOrganizationsCountUseCase getTotalOrganizationsCountUseCase,
        IGetOrganizationsDashboardSummaryUseCase getOrganizationsDashboardSummaryUseCase,
        IGetRecentOrganizationsUseCase getRecentOrganizationsUseCase,
        IGetOrganizationsUseCase? getOrganizationsUseCase = null,
        IGetOrganizationCentersUseCase? getOrganizationCentersUseCase = null,
        ICurrentUserService? currentUserService = null)
    {
        _createOrganizationUseCase = createOrganizationUseCase ?? throw new ArgumentNullException(nameof(createOrganizationUseCase));
        _getTotalOrganizationsCountUseCase = getTotalOrganizationsCountUseCase ?? throw new ArgumentNullException(nameof(getTotalOrganizationsCountUseCase));
        _getOrganizationsDashboardSummaryUseCase = getOrganizationsDashboardSummaryUseCase ?? throw new ArgumentNullException(nameof(getOrganizationsDashboardSummaryUseCase));
        _getRecentOrganizationsUseCase = getRecentOrganizationsUseCase ?? throw new ArgumentNullException(nameof(getRecentOrganizationsUseCase));
        _getOrganizationsUseCase = getOrganizationsUseCase;
        _getOrganizationCentersUseCase = getOrganizationCentersUseCase;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrganizations(CancellationToken cancellationToken)
    {
        if (_getOrganizationsUseCase == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Service not configured.");
        }

        var result = await _getOrganizationsUseCase.ExecuteAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{organizationId:guid}/centers")]
    [Authorize(Roles = "CenterManager,SystemAdmin")]
    [ProducesResponseType(typeof(IEnumerable<BranchListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationCenters(
        [FromRoute] Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (_getOrganizationCentersUseCase == null || _currentUserService == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Service not configured.");
        }

        bool isSystemAdmin = _currentUserService.IsInRole("SystemAdmin");
        Guid? userOrgId = _currentUserService.OrganizationId;

        try
        {
            var result = await _getOrganizationCentersUseCase.ExecuteAsync(
                organizationId,
                isSystemAdmin,
                userOrgId,
                cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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

    [HttpGet("dashboard-summary")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(typeof(OrganizationsDashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var result = await _getOrganizationsDashboardSummaryUseCase.ExecuteAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("recent")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(typeof(IEnumerable<RecentOrganizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecent(CancellationToken cancellationToken)
    {
        var result = await _getRecentOrganizationsUseCase.ExecuteAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("count")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(typeof(OrganizationCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCount(CancellationToken cancellationToken)
    {
        var result = await _getTotalOrganizationsCountUseCase.ExecuteAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
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
