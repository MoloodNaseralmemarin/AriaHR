using AriaHR.Shared;

namespace AriaHR.Modules.Reporting.Domain.Entities;

/// <summary>
/// AuditLog entity recording generic system audit trails.
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IPAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
