using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.DTOs;

public class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OrganizationType Type { get; set; }
    public string? NationalIdentifier { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
