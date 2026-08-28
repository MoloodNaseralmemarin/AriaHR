using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;

namespace AriaHR.Modules.Organization.Application.UseCases.GetOrganizationsDashboardSummary;

public class GetOrganizationsDashboardSummaryUseCase : IGetOrganizationsDashboardSummaryUseCase
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetOrganizationsDashboardSummaryUseCase(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
    }

    public async Task<OrganizationsDashboardSummaryResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var (totalCount, createdThisMonth) = await _organizationRepository.GetDashboardSummaryAsync(cancellationToken);

        return new OrganizationsDashboardSummaryResponse
        {
            TotalCount = totalCount,
            CreatedThisMonth = createdThisMonth
        };
    }
}
