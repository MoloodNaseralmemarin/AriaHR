using AriaHR.Modules.Requests.Domain.Entities;

namespace AriaHR.Modules.Requests.Application.Repositories;

public interface ILeaveRequestRepository
{
    Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetPendingRequestsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetPendingRequestsForApproverAsync(Guid approverId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetApprovedRequestsByDateRangeAsync(Guid organizationId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);
}
