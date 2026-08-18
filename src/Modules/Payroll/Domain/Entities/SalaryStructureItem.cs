using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// SalaryStructureItem entity linking SalaryStructure to SalaryComponent.
/// </summary>
public class SalaryStructureItem : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid SalaryStructureId { get; set; }
    public SalaryStructure? SalaryStructure { get; set; }

    public Guid SalaryComponentId { get; set; }
    public SalaryComponent? SalaryComponent { get; set; }

    public string CalculationType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public bool IsActive { get; set; }
}
