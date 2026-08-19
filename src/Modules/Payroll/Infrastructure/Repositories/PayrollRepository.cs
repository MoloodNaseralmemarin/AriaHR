using AriaHR.Modules.Payroll.Application.Repositories;
using AriaHR.Modules.Payroll.Domain.Entities;
using AriaHR.Modules.Payroll.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Payroll.Infrastructure.Repositories;

/// <summary>
/// EF Core repository implementation for Payroll persistence operations required by UC-801.
/// </summary>
public class PayrollRepository : IPayrollRepository
{
    private readonly PayrollDbContext _context;

    public PayrollRepository(PayrollDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddAsync(PayrollRecord payrollRecord, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payrollRecord);
        await _context.PayrollRecords.AddAsync(payrollRecord, cancellationToken);
    }

    public async Task<PayrollRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.PayrollPeriod)
            .Include(r => r.PayrollItems)
            .Include(r => r.InsuranceRecord)
            .Include(r => r.TaxRecord)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<PayrollRecord?> GetByEmployeeAndPeriodAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        return await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.PayrollPeriod)
            .Include(r => r.PayrollItems)
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId &&
                                      r.PayrollPeriod != null &&
                                      r.PayrollPeriod.Year == year &&
                                      r.PayrollPeriod.Month == month, cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollRecord>> GetByPeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var records = await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.PayrollPeriod)
            .Where(r => r.PayrollPeriod != null &&
                        r.PayrollPeriod.Year == year &&
                        r.PayrollPeriod.Month == month)
            .ToListAsync(cancellationToken);

        return records;
    }

    public async Task<IReadOnlyList<PayrollRecord>> GetByEmployeeIdsAndPeriodAsync(
        IReadOnlyCollection<Guid> employeeIds,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employeeIds);

        if (employeeIds.Count == 0)
        {
            return Array.Empty<PayrollRecord>();
        }

        var records = await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.PayrollPeriod)
            .Where(r => employeeIds.Contains(r.EmployeeId) &&
                        r.PayrollPeriod != null &&
                        r.PayrollPeriod.Year == year &&
                        r.PayrollPeriod.Month == month)
            .ToListAsync(cancellationToken);

        return records;
    }

    public Task UpdateAsync(PayrollRecord payrollRecord, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payrollRecord);
        _context.PayrollRecords.Update(payrollRecord);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsForEmployeeAndPeriodAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        return await _context.PayrollRecords
            .AsNoTracking()
            .AnyAsync(r => r.EmployeeId == employeeId &&
                           r.PayrollPeriod != null &&
                           r.PayrollPeriod.Year == year &&
                           r.PayrollPeriod.Month == month, cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollRecord>> GetByStatusAndPeriodAsync(
        string status,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var records = await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.PayrollPeriod)
            .Where(r => r.Status == status &&
                        r.PayrollPeriod != null &&
                        r.PayrollPeriod.Year == year &&
                        r.PayrollPeriod.Month == month)
            .ToListAsync(cancellationToken);

        return records;
    }
}
