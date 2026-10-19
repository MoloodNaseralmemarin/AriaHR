using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.DTOs;

public class RecentCenterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OrganizationType Type { get; set; }
    public bool IsActive { get; set; }
    public bool IsAwaitingCompletion { get; set; }
    public string? ManagerName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
