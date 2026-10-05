using AriaHR.Modules.Requests.Domain.Entities;

namespace AriaHR.Modules.Requests.Application.Repositories;

public interface ILeaveTypeRepository
{
    Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeaveType?> GetByIdAndOrganizationIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveType>> GetAllByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveType>> GetAllActiveAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(Guid organizationId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaveType leaveType, CancellationToken cancellationToken = default);
}
