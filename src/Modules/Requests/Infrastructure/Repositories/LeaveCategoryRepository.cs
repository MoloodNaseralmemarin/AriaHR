using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Modules.Requests.Domain.Entities;
using AriaHR.Modules.Requests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Requests.Infrastructure.Repositories;

public class LeaveCategoryRepository : ILeaveCategoryRepository
{
    private readonly RequestsDbContext _dbContext;

    public LeaveCategoryRepository(RequestsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<LeaveCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(lt => lt.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveCategory>> GetAllActiveAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveCategories
            .AsNoTracking()
            .Where(lt => lt.OrganizationId == organizationId && lt.IsActive)
            .ToListAsync(cancellationToken);
    }
}
