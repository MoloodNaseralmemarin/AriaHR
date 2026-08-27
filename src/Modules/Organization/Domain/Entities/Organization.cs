using AriaHR.Shared;

namespace AriaHR.Modules.Organization.Domain.Entities;

/// <summary>
/// Organization entity representing a tenant.
/// </summary>
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? NationalIdentifier { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public string? ManagerFirstName { get; set; }

    public string? ManagerLastName { get; set; }

    public string? ManagerMobile { get; set; }
    public bool IsActive { get; set; }
    public OrganizationType Type { get; set; }

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
