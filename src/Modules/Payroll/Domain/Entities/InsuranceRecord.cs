using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// InsuranceRecord entity snapshotting insurance contribution calculations for a payroll record.
/// </summary>
public class InsuranceRecord : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid PayrollRecordId { get; set; }
    public PayrollRecord? PayrollRecord { get; set; }

    public Guid InsuranceRuleId { get; set; }
    public InsuranceRule? InsuranceRule { get; set; }

    public decimal InsuranceableAmount { get; set; }
    public decimal EmployeeRate { get; set; }
    public decimal EmployerRate { get; set; }
    public decimal EmployeeAmount { get; set; }
    public decimal EmployerAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
