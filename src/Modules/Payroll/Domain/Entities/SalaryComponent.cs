using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// SalaryComponent entity defining salary earning or deduction items.
/// </summary>
public class SalaryComponent : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string CalculationType { get; set; } = string.Empty;
    public bool IsTaxable { get; set; }
    public bool IsInsuranceable { get; set; }
    public bool IsActive { get; set; }

    public ICollection<SalaryStructureItem> SalaryStructureItems { get; set; } = new List<SalaryStructureItem>();
    public ICollection<PayrollItem> PayrollItems { get; set; } = new List<PayrollItem>();
}
