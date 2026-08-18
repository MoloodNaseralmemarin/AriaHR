using AriaHR.Shared;

namespace AriaHR.Modules.Scheduling.Domain.Entities;

/// <summary>
/// Holiday entity representing official calendar holidays.
/// </summary>
public class Holiday : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public DateOnly Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsNational { get; set; }
}
