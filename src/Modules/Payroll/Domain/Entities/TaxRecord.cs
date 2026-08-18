using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// TaxRecord entity snapshotting progressive tax calculations for a payroll record.
/// </summary>
public class TaxRecord : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid PayrollRecordId { get; set; }
    public PayrollRecord? PayrollRecord { get; set; }

    public Guid TaxRuleId { get; set; }
    public TaxRule? TaxRule { get; set; }

    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
}
