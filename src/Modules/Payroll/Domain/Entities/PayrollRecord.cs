using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// PayrollRecord entity representing monthly payroll snapshot for an employee.
/// </summary>
public class PayrollRecord : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }

    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public decimal GrossAmount { get; set; }
    public decimal InsuranceableAmount { get; set; }
    public decimal EmployeeInsurance { get; set; }
    public decimal EmployerInsurance { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }

    public ICollection<PayrollItem> PayrollItems { get; set; } = new List<PayrollItem>();
    public InsuranceRecord? InsuranceRecord { get; set; }
    public TaxRecord? TaxRecord { get; set; }
}
