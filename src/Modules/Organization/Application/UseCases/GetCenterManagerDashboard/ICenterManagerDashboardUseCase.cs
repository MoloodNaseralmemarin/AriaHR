using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetCenterManagerDashboard;

public interface ICenterManagerDashboardUseCase
{
    Task<CenterManagerDashboardResponse> ExecuteAsync(CancellationToken cancellationToken = default);
}
