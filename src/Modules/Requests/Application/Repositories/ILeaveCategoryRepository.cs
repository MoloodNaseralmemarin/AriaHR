using AriaHR.Modules.Requests.Domain.Entities;

namespace AriaHR.Modules.Requests.Application.Repositories;

public interface ILeaveCategoryRepository
{
    Task<LeaveCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveCategory>> GetAllActiveAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveCategory>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<LeaveCategory?> GetByNameAndOrganizationAsync(string name, Guid organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveCategory category, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaveCategory category, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
