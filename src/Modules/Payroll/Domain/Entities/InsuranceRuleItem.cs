using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// InsuranceRuleItem entity defining specific share percentages for an InsuranceRule.
/// </summary>
public class InsuranceRuleItem : BaseEntity
{
    public Guid InsuranceRuleId { get; set; }
    public InsuranceRule? InsuranceRule { get; set; }

    public string ComponentType { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsEmployeeShare { get; set; }
    public bool IsEmployerShare { get; set; }
    public bool IsActive { get; set; }
}
