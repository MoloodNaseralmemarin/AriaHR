using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Repositories;

namespace AriaHR.Modules.Identity.Application.UseCases;

public interface IGetCenterManagersUseCase
{
    Task<IReadOnlyList<CenterManagerDto>> ExecuteAsync(Guid? organizationId = null, CancellationToken cancellationToken = default);
}

public class GetCenterManagersUseCase : IGetCenterManagersUseCase
{
    private readonly IUserRepository _userRepository;

    public GetCenterManagersUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<IReadOnlyList<CenterManagerDto>> ExecuteAsync(Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetCenterManagersAsync(organizationId, cancellationToken);
    }
}
