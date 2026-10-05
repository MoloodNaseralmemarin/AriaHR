using AriaHR.Shared;

namespace AriaHR.Modules.Requests.Domain.Entities;

/// <summary>
/// LeaveCategory entity representing categories of available leave (e.g., Annual, Sick).
/// </summary>
public class LeaveCategory : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxDaysPerYear { get; set; }
    public bool IsPaid { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool IsActive { get; set; }

    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
}
