using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Shared.Services;

namespace AriaHR.Modules.Identity.Application.UseCases;

public class GetCurrentUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IOrganizationLookupService? _organizationLookupService;

    public GetCurrentUserUseCase(
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IOrganizationLookupService? organizationLookupService = null)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _organizationLookupService = organizationLookupService;
    }

    public async Task<CurrentUserResponse?> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        var roles = await _userRoleRepository.GetRolesByUserIdAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();

        string? orgName = null;
        if (_organizationLookupService != null)
        {
            orgName = await _organizationLookupService.GetOrganizationNameForUserAsync(
                user.OrganizationId,
                user.PhoneNumber,
                user.Id,
                cancellationToken);
        }

        return new CurrentUserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            roleNames,
            orgName);
    }
}
