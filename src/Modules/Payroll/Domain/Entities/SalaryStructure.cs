using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// SalaryStructure entity grouping salary components into salary packages.
/// </summary>
public class SalaryStructure : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public ICollection<SalaryStructureItem> SalaryStructureItems { get; set; } = new List<SalaryStructureItem>();
    public ICollection<EmployeeSalary> EmployeeSalaries { get; set; } = new List<EmployeeSalary>();
}
