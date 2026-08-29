using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetDashboardSummary;

public interface IGetOrganizationsDashboardSummaryUseCase
{
    Task<OrganizationsDashboardSummaryResponse> ExecuteAsync(CancellationToken cancellationToken = default);
}
