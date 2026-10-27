using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.Services;

public class EmployeeLookupService : IEmployeeLookupService
{
    private readonly OrganizationDbContext _organizationDbContext;
    private readonly IdentityDbContext _identityDbContext;

    public EmployeeLookupService(
        OrganizationDbContext organizationDbContext,
        IdentityDbContext identityDbContext)
    {
        _organizationDbContext = organizationDbContext ?? throw new ArgumentNullException(nameof(organizationDbContext));
        _identityDbContext = identityDbContext ?? throw new ArgumentNullException(nameof(identityDbContext));
    }

    public async Task<(Guid EmployeeId, Guid OrganizationId)?> GetEmployeeDetailsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var employee = await _organizationDbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted && e.IsActive, cancellationToken);

        if (employee == null)
        {
            return null;
        }

        return (employee.Id, employee.OrganizationId);
    }

    public async Task<IReadOnlyDictionary<Guid, (string FirstName, string LastName, string FullName)>> GetEmployeeNamesByIdsAsync(
        IEnumerable<Guid> employeeIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, (string FirstName, string LastName, string FullName)>();

        if (employeeIds == null)
        {
            return result;
        }

        var distinctIds = employeeIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (!distinctIds.Any())
        {
            return result;
        }

        var employees = await _organizationDbContext.Employees
            .AsNoTracking()
            .Where(e => distinctIds.Contains(e.Id) && !e.IsDeleted)
            .Select(e => new { e.Id, e.UserId })
            .ToListAsync(cancellationToken);

        if (!employees.Any())
        {
            return result;
        }

        var userIds = employees.Select(e => e.UserId).Distinct().ToList();

        var users = await _identityDbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        foreach (var employee in employees)
        {
            if (users.TryGetValue(employee.UserId, out var user))
            {
                string firstName = user.FirstName ?? string.Empty;
                string lastName = user.LastName ?? string.Empty;
                string fullName = $"{firstName} {lastName}".Trim();
                result[employee.Id] = (firstName, lastName, fullName);
            }
        }

        return result;
    }
}
