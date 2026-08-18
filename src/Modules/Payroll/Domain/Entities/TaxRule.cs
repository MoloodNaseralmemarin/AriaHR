using AriaHR.Shared;

namespace AriaHR.Modules.Payroll.Domain.Entities;

/// <summary>
/// TaxRule entity defining global legal tax regulations for a period.
/// </summary>
public class TaxRule : BaseEntity
{
    public int Year { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly EffectiveTo { get; set; }
    public bool IsActive { get; set; }

    public ICollection<TaxBracket> TaxBrackets { get; set; } = new List<TaxBracket>();
    public ICollection<TaxRecord> TaxRecords { get; set; } = new List<TaxRecord>();
}
