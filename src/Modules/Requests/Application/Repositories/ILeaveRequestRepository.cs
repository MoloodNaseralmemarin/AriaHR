using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Domain.Enums;

namespace AriaHR.Modules.Requests.Application.Repositories;

public interface ILeaveRequestRepository
{
    Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetOrganizationRequestsAsync(
        Guid organizationId,
        RequestStatus? status = null,
        Guid? employeeId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> HasConflictingLeaveRequestAsync(
        Guid employeeId,
        Guid organizationId,
        LeaveType leaveType,
        DateOnly startDate,
        DateOnly endDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        Guid? excludeRequestId = null,
        CancellationToken cancellationToken = default);

    bool IsRelational();
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
