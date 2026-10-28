namespace AriaHR.Shared.Services;

public record ApprovedLeaveInfoDto(
    bool IsOnApprovedLeave,
    string? LeaveType,
    TimeOnly? StartTime,
    TimeOnly? EndTime
);

public interface ILeaveLookupService
{
    /// <summary>
    /// Checks if an employee is currently on approved leave at the specified UTC timestamp (converted to Iran local time).
    /// </summary>
    Task<bool> IsEmployeeOnApprovedLeaveAsync(Guid employeeId, DateTime utcTimestamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a set of employee IDs for active employees who are on approved leave in the target organization on the specified local date.
    /// </summary>
    Task<HashSet<Guid>> GetEmployeeIdsOnApprovedLeaveTodayAsync(Guid organizationId, DateOnly localDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active leave status for a dictionary of employee IDs on a target UTC timestamp (converted to Iran local time).
    /// </summary>
    Task<Dictionary<Guid, ApprovedLeaveInfoDto>> GetEmployeesApprovedLeaveStatusAsync(Guid organizationId, DateTime utcTimestamp, CancellationToken cancellationToken = default);
}
