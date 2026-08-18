using AriaHR.Shared;

namespace AriaHR.Modules.Scheduling.Domain.Entities;

/// <summary>
/// ShiftAssignment entity linking an Employee to a Shift for a specific Date.
/// </summary>
public class ShiftAssignment : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }

    public Guid ShiftId { get; set; }
    public Shift? Shift { get; set; }

    public DateOnly Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }
}
