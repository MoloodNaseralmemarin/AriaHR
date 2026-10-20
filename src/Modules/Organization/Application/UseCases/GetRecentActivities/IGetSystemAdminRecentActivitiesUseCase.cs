using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetRecentActivities;

public interface IGetSystemAdminRecentActivitiesUseCase
{
    Task<PagedResult<RecentActivityDto>> ExecuteAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
}
