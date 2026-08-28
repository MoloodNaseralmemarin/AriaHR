using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.UseCases.GetRecentActivities;

public interface IGetRecentActivitiesUseCase
{
    Task<IReadOnlyList<RecentActivityDto>> ExecuteAsync(CancellationToken cancellationToken = default);
}
