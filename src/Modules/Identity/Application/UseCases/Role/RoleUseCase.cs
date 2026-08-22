using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.UseCases.Role;

public class RoleUseCase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public RoleUseCase(
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<RoleResponse?> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return null;
        }

        var existingRole = await _roleRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existingRole != null)
        {
            return null;
        }

        var role = new Domain.Entities.Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _roleRepository.AddAsync(role, cancellationToken);

        return new RoleResponse(role.Id, role.Name, role.Description);
    }

    public async Task<IReadOnlyList<RoleResponse>> GetAllRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        return roles.Select(r => new RoleResponse(r.Id, r.Name, r.Description)).ToList();
    }

    public async Task<bool> AssignRoleAsync(
        AssignRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.RoleName))
        {
            return false;
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        var role = await _roleRepository.GetByNameAsync(request.RoleName, cancellationToken);
        if (role == null)
        {
            return false;
        }

        bool hasRole = await _userRoleRepository.UserHasRoleAsync(user.Id, role.Id, cancellationToken);
        if (hasRole)
        {
            return true;
        }

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = role.Id,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _userRoleRepository.AddAsync(userRole, cancellationToken);

        return true;
    }
}
