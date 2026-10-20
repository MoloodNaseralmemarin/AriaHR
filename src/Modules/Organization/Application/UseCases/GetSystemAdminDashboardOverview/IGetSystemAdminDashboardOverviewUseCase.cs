using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetSystemAdminDashboardOverview;

public interface IGetSystemAdminDashboardOverviewUseCase
{
    Task<SystemAdminDashboardOverviewDto> ExecuteAsync(CancellationToken cancellationToken = default);
}
