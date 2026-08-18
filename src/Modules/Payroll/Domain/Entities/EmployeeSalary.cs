using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// EmployeeSalary entity storing employee salary structure assignments over time.
/// </summary>
public class EmployeeSalary : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }

    public Guid SalaryStructureId { get; set; }
    public SalaryStructure? SalaryStructure { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public decimal BaseAmount { get; set; }
    public bool IsActive { get; set; }
}
