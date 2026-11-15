using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Organization.Infrastructure.Repositories;

public class WorkLocationRepository : IWorkLocationRepository
{
    private readonly OrganizationDbContext _dbContext;

    public WorkLocationRepository(OrganizationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(WorkLocation workLocation, CancellationToken cancellationToken = default)
    {
        await _dbContext.WorkLocations.AddAsync(workLocation, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(o => !o.IsDeleted && o.IsActive && o.Id == organizationId, cancellationToken);
    }

    public async Task<WorkLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(w => !w.IsDeleted && w.Id == id, cancellationToken);
    }

    public async Task<WorkLocation?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkLocations
            .FirstOrDefaultAsync(w => !w.IsDeleted && w.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkLocation>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkLocations
            .AsNoTracking()
            .Where(w => !w.IsDeleted && w.OrganizationId == organizationId)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public void Update(WorkLocation workLocation)
    {
        _dbContext.WorkLocations.Update(workLocation);
    }
}
