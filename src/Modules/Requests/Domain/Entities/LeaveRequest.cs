using AriaHR.Modules.Requests.Domain.Enums;
using AriaHR.Shared;


namespace AriaHR.Modules.Requests.Domain.Entities;

/// <summary>
/// Represents an employee leave request.
/// LeaveType defines the duration mode (hourly or daily),
/// while LeaveCategoryId identifies the organization's leave category.
/// </summary>
public class LeaveRequest : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid EmployeeId { get; set; }

    /// <summary>
    /// The duration mode: Hourly or Daily.
    /// </summary>
    public LeaveType LeaveType { get; set; }

    /// <summary>
    /// References the organization's LeaveCategory.
    /// No cross-module EF navigation property is defined.
    /// </summary>
    public Guid LeaveCategoryId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Required for hourly requests; null for daily requests.
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// Required for hourly requests; null for daily requests.
    /// </summary>
    public TimeOnly? EndTime { get; set; }

    public string? Reason { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public string? RejectedReason { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? ApprovedBy { get; set; }
}