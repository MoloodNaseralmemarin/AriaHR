using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;

public class GetOrganizationsDashboardSummaryUseCase : IGetOrganizationsDashboardSummaryUseCase
{
    private readonly IUserRepository _userRepository;

    public GetOrganizationsDashboardSummaryUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<OrganizationsDashboardSummaryResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var centerManagerCount = await _userRepository.GetCountByRoleNameAsync("CenterManager", cancellationToken);

        return new OrganizationsDashboardSummaryResponse
        {
            CenterManagerCount = centerManagerCount
        };
    }
}
