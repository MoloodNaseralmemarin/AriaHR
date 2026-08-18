using AriaHR.Shared;

namespace AriaHR.Modules.Requests.Domain.Entities;

/// <summary>
/// MissionRequest entity representing official work assignments/travel requests.
/// </summary>
public class MissionRequest : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public string? Purpose { get; set; }
    public DateTime FromDateTime { get; set; }
    public DateTime ToDateTime { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ApproverId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MissionLocationLog> MissionLocationLogs { get; set; } = new List<MissionLocationLog>();
}
