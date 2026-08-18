using AriaHR.Shared;

namespace AriaHR.Modules.Scheduling.Domain.Entities;

/// <summary>
/// ShiftSwapRequest entity for requesting shift exchanges between employees.
/// </summary>
public class ShiftSwapRequest : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid TargetEmployeeId { get; set; }
    public Guid OriginalAssignmentId { get; set; }
    public Guid RequestedAssignmentId { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ApproverId { get; set; }
}
