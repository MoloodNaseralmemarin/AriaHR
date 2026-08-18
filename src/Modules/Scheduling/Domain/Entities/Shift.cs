using AriaHR.Shared;

namespace AriaHR.Modules.Scheduling.Domain.Entities;

/// <summary>
/// Shift entity representing work schedule shift definitions.
/// </summary>
public class Shift : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int BreakDurationMinutes { get; set; }
    public string? ColorCode { get; set; }
    public bool IsActive { get; set; }

    public ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();
}
