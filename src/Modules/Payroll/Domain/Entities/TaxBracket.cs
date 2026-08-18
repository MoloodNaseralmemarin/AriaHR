using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// TaxBracket entity defining progressive tax brackets for a TaxRule.
/// </summary>
public class TaxBracket : BaseEntity
{
    public Guid TaxRuleId { get; set; }
    public TaxRule? TaxRule { get; set; }

    public decimal FromAmount { get; set; }
    public decimal? ToAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal FixedAmount { get; set; }
}
