using AriaHR.Shared;

namespace AriaHR.Modules.Organization.Domain.Entities;

/// <summary>
/// Department entity representing internal organizational hierarchy.
/// </summary>
public class Department : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();

    public bool IsActive { get; set; }
}
