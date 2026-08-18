using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// PayrollItem entity storing granular salary item components for a payroll record snapshot.
/// </summary>
public class PayrollItem : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid PayrollRecordId { get; set; }
    public PayrollRecord? PayrollRecord { get; set; }

    public Guid SalaryComponentId { get; set; }
    public SalaryComponent? SalaryComponent { get; set; }

    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}
