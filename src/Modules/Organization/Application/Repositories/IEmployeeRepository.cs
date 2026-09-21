using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.Repositories;

public interface IEmployeeRepository
{
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByPersonnelCodeAsync(string personnelCode, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
