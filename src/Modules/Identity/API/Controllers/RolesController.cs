using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.UseCases.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Identity.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly RoleUseCase _roleUseCase;

    public RolesController(RoleUseCase roleUseCase)
    {
        _roleUseCase = roleUseCase;
    }

    [HttpPost("api/identity/roles")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roleUseCase.CreateRoleAsync(request, cancellationToken);
        if (result == null)
        {
            return BadRequest(new { Message = "Role creation failed. Duplicate role name or invalid request." });
        }

        return CreatedAtAction(nameof(GetRoles), new { id = result.Id }, result);
    }

    [HttpGet("api/identity/roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _roleUseCase.GetAllRolesAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpPost("api/identity/users/assign-role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRole(
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _roleUseCase.AssignRoleAsync(request, cancellationToken);
        if (!success)
        {
            return BadRequest(new { Message = "Role assignment failed. Invalid user ID or role name." });
        }

        return Ok(new { Message = "Role assigned successfully." });
    }
}
