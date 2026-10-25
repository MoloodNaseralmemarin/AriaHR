using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.Services;

public class OrganizationLookupService : IOrganizationLookupService
{
    private readonly OrganizationDbContext _organizationDbContext;

    public OrganizationLookupService(OrganizationDbContext organizationDbContext)
    {
        _organizationDbContext = organizationDbContext ?? throw new ArgumentNullException(nameof(organizationDbContext));
    }

    public async Task<string?> GetOrganizationNameByIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            return null;
        }

        return await _organizationDbContext.Organizations
            .AsNoTracking()
            .Where(o => o.Id == organizationId && !o.IsDeleted)
            .Select(o => o.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string?> GetOrganizationNameForUserAsync(
        Guid? organizationId,
        string? phoneNumber,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId.HasValue && organizationId.Value != Guid.Empty)
        {
            var name = await GetOrganizationNameByIdAsync(organizationId.Value, cancellationToken);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var normalizedPhone = phoneNumber.Trim();
            var matchingOrgs = await _organizationDbContext.Organizations
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.ManagerMobile != null && o.ManagerMobile == normalizedPhone)
                .Select(o => o.Name)
                .ToListAsync(cancellationToken);

            if (matchingOrgs.Count == 1)
            {
                return matchingOrgs.Single();
            }
        }

        if (userId != Guid.Empty)
        {
            var empOrgName = await _organizationDbContext.Employees
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.UserId == userId && e.Organization != null && !e.Organization.IsDeleted)
                .Select(e => e.Organization!.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrEmpty(empOrgName))
            {
                return empOrgName;
            }
        }

        return null;
    }
}
