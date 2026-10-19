using AriaHR.Modules.Organization.Application.DTOs;

namespace AriaHR.Modules.Organization.Application.Repositories;

public interface IOrganizationRepository
{
    Task AddAsync(Domain.Entities.Organization organization, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<(int TotalCount, int CreatedThisMonth)> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.Organization>> GetRecentOrganizationsAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentActivityDto>> GetRecentActivitiesAsync(int count, CancellationToken cancellationToken = default);
    Task<(int TotalCount, int ActiveCount, int AwaitingCompletionCount)> GetCentersOverviewAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<RecentCenterDto>> GetRecentCentersPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<RecentActivityDto>> GetRecentActivitiesPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
