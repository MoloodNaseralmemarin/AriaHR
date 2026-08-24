using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Repositories;

namespace AriaHR.Modules.Identity.Application.UseCases;

public class GetCurrentUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public GetCurrentUserUseCase(
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<UserResponse?> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        var roles = await _userRoleRepository.GetRolesByUserIdAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();

        return new UserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            roleNames);
    }
}
