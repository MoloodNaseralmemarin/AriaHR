using AriaHR.Modules.Requests.Domain.Entities;

namespace AriaHR.Modules.Requests.Application.Repositories;

public interface ILeaveTypeRepository
{
    Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveType>> GetAllActiveAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
