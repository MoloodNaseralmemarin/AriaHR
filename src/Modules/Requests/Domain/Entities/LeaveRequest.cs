using AriaHR.Shared;

namespace AriaHR.Modules.Requests.Domain.Entities;

/// <summary>
/// LeaveRequest entity representing leave applications.
/// </summary>
public class LeaveRequest : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }

    public Guid LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }

    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public TimeOnly? FromTime { get; set; }
    public TimeOnly? ToTime { get; set; }
    public decimal TotalDays { get; set; }
    public string? Reason { get; set; }
    public string? AttachmentPath { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ApproverId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
