using AriaHR.Modules.Organization.Domain.Enums;

namespace AriaHR.Modules.Organization.Application.DTOs;

public class OrganizationListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OrganizationType OrganizationType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
