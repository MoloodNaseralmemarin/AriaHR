using AriaHR.Shared;

namespace AriaHR.Modules.Organization.Domain.Entities;

/// <summary>
/// Branch entity belonging to an Organization.
/// </summary>
public class Branch : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }

    public ICollection<WorkLocation> WorkLocations { get; set; } = new List<WorkLocation>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
