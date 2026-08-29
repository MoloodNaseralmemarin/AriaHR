using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.DTOs;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? NationalIdentifier { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public string? ManagerFirstName { get; set; }

    public string? ManagerLastName { get; set; }

    public string? ManagerMobile { get; set; }

    public string? ManagerEmail { get; set; }
    public bool IsActive { get; set; }

    public OrganizationType Type { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
}
