using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// InsuranceRule entity defining global legal insurance regulations for a period.
/// </summary>
public class InsuranceRule : BaseEntity
{
    public int Year { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly EffectiveTo { get; set; }
    public bool IsActive { get; set; }

    public ICollection<InsuranceRuleItem> InsuranceRuleItems { get; set; } = new List<InsuranceRuleItem>();
    public ICollection<InsuranceRecord> InsuranceRecords { get; set; } = new List<InsuranceRecord>();
}
