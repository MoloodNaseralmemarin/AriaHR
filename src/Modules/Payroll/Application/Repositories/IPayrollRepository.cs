using AriaHR.Modules.Payroll.Domain.Entities;

namespace AriaHR.Modules.Payroll.Application.Repositories;

/// <summary>
/// Repository abstraction for Payroll persistence operations required by UC-801.
/// </summary>
public interface IPayrollRepository
{
    Task AddAsync(PayrollRecord payrollRecord, CancellationToken cancellationToken = default);

    Task<PayrollRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PayrollRecord?> GetByEmployeeAndPeriodAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PayrollRecord>> GetByPeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PayrollRecord>> GetByEmployeeIdsAndPeriodAsync(
        IReadOnlyCollection<Guid> employeeIds,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        PayrollRecord payrollRecord,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForEmployeeAndPeriodAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PayrollRecord>> GetByStatusAndPeriodAsync(
        string status,
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
