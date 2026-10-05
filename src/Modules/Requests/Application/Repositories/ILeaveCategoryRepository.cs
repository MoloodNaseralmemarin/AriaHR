using AriaHR.Modules.Requests.Domain.Entities;

namespace AriaHR.Modules.Requests.Application.Repositories;

public interface ILeaveCategoryRepository
{
    Task<LeaveCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveCategory>> GetAllActiveAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
