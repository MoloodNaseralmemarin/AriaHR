using AriaHR.Modules.Requests.Domain.Entities;

namespace AriaHR.Modules.Requests.Application.Repositories;

public interface ILeaveBalanceRepository
{
    Task AddAsync(LeaveBalance leaveBalance, CancellationToken cancellationToken = default);
    Task<LeaveBalance?> GetByEmployeeAndLeaveTypeAsync(Guid employeeId, Guid leaveTypeId, int year, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveBalance>> GetByEmployeeAndYearAsync(Guid employeeId, int year, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaveBalance leaveBalance, CancellationToken cancellationToken = default);
}
