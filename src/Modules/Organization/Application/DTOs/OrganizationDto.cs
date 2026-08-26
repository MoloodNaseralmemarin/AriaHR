using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.DTOs;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OrganizationType Type { get; set; }
    public string? NationalIdentifier { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
}
