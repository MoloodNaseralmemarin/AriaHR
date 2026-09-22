using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.Repositories;

public interface IWorkLocationRepository
{
    Task AddAsync(WorkLocation workLocation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<WorkLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
