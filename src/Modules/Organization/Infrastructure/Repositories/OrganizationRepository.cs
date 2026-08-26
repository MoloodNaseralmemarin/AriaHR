using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Infrastructure.Persistence;

namespace AriaHR.Modules.Organization.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly OrganizationDbContext _dbContext;

    public OrganizationRepository(OrganizationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(Domain.Entities.Organization organization, CancellationToken cancellationToken = default)
    {
        await _dbContext.Organizations.AddAsync(organization, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
