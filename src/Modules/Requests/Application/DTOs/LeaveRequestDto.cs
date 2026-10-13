using AriaHR.Modules.Requests.Domain.Enums;

namespace AriaHR.Modules.Requests.Application.DTOs;

public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public LeaveType LeaveType { get; set; }
    public Guid LeaveCategoryId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Reason { get; set; }
    public RequestStatus Status { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
