using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.Repositories;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Branch>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(Branch branch, CancellationToken cancellationToken = default);
    Task UpdateAsync(Branch branch, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
