using AriaHR.Modules.Scheduling.Domain.Entities;

namespace AriaHR.Modules.Scheduling.Application.Repositories;

public interface IShiftRepository
{
    Task AddAsync(Shift shift, CancellationToken cancellationToken = default);
    Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shift>> GetActiveShiftsByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken = default);
    Task UpdateAsync(Shift shift, CancellationToken cancellationToken = default);
}
