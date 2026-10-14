using AriaHR.Shared;

namespace AriaHR.Modules.Notification.Domain.Entities;

/// <summary>
/// Domain entity representing explicit permission granted by SystemAdmin
/// for cross-center communication from a source organization to a destination organization.
/// </summary>
public class CrossCenterPermission : BaseEntity
{
    public Guid SourceOrganizationId { get; set; }
    public Guid DestinationOrganizationId { get; set; }
    public Guid GrantedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
}
