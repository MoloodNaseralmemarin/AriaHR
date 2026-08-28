using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.Repositories;

public interface IOrganizationRepository
{
    Task AddAsync(Domain.Entities.Organization organization, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<(int TotalCount, int CreatedThisMonth)> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.Organization>> GetRecentOrganizationsAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DTOs.RecentActivityDto>> GetRecentActivitiesAsync(int count, CancellationToken cancellationToken = default);
}
