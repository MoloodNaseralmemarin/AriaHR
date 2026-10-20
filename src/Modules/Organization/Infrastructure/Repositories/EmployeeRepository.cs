using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Shared.Utils;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly OrganizationDbContext _dbContext;

    public EmployeeRepository(OrganizationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _dbContext.Employees.AddAsync(employee, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByPersonnelCodeAsync(string personnelCode, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .AnyAsync(e => !e.IsDeleted && e.OrganizationId == organizationId && e.PersonnelCode == personnelCode, cancellationToken);
    }

    public async Task<bool> ExistsByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .AnyAsync(e => !e.IsDeleted && e.NationalCode == nationalCode, cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .AnyAsync(e => !e.IsDeleted && e.IsActive && e.UserId == userId, cancellationToken);
    }

    public async Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(o => !o.IsDeleted && o.IsActive && o.Id == organizationId, cancellationToken);
    }

    public async Task<(int TotalCount, int CreatedThisMonth)> GetEmployeesSummaryAsync(DateTime? referenceDateUtc = null, CancellationToken cancellationToken = default)
    {
        var (startOfMonthUtc, startOfNextMonthUtc) = JalaliCalendarHelper.GetCurrentJalaliMonthBoundariesUtc(referenceDateUtc);

        var nonDeletedEmployees = _dbContext.Employees
            .AsNoTracking()
            .Where(e => !e.IsDeleted);

        var totalCount = await nonDeletedEmployees.CountAsync(cancellationToken);

        var createdThisMonth = await nonDeletedEmployees
            .Where(e => e.CreatedAtUtc >= startOfMonthUtc && e.CreatedAtUtc < startOfNextMonthUtc)
            .CountAsync(cancellationToken);

        return (totalCount, createdThisMonth);
    }
}
