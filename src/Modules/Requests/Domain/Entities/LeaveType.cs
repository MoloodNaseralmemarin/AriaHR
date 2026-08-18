using AriaHR.Shared;

namespace AriaHR.Modules.Requests.Domain.Entities;

/// <summary>
/// LeaveType entity representing types of available leave (e.g., Annual, Sick).
/// </summary>
public class LeaveType : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxDaysPerYear { get; set; }
    public bool IsPaid { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool IsActive { get; set; }

    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
