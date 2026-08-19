using AriaHR.Modules.Requests.Domain.Entities;

namespace AriaHR.Modules.Requests.Application.Repositories;

public interface IMissionRequestRepository
{
    Task AddAsync(MissionRequest missionRequest, CancellationToken cancellationToken = default);
    Task<MissionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MissionRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MissionRequest>> GetPendingRequestsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MissionRequest>> GetPendingRequestsForApproverAsync(Guid approverId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MissionRequest>> GetApprovedRequestsByDateRangeAsync(Guid organizationId, DateTime startDateTime, DateTime endDateTime, CancellationToken cancellationToken = default);
    Task UpdateAsync(MissionRequest missionRequest, CancellationToken cancellationToken = default);
}
