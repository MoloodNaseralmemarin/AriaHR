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
}
