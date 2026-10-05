using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.Services;

public class EmployeeLookupService : IEmployeeLookupService
{
    private readonly OrganizationDbContext _organizationDbContext;

    public EmployeeLookupService(OrganizationDbContext organizationDbContext)
    {
        _organizationDbContext = organizationDbContext ?? throw new ArgumentNullException(nameof(organizationDbContext));
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
}
