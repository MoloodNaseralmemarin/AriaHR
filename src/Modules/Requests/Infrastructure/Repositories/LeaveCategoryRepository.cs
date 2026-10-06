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
            .FirstOrDefaultAsync(lt => lt.Id == id && !lt.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveCategory>> GetAllActiveAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveCategories
            .AsNoTracking()
            .Where(lt => lt.OrganizationId == organizationId && lt.IsActive && !lt.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveCategory>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveCategories
            .AsNoTracking()
            .Where(lt => lt.OrganizationId == organizationId && !lt.IsDeleted)
            .OrderBy(lt => lt.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveCategory?> GetByNameAndOrganizationAsync(string name, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        return await _dbContext.LeaveCategories
            .FirstOrDefaultAsync(lt => lt.OrganizationId == organizationId && !lt.IsDeleted && lt.Name.ToLower() == trimmedName.ToLower(), cancellationToken);
    }

    public async Task AddAsync(LeaveCategory category, CancellationToken cancellationToken = default)
    {
        await _dbContext.LeaveCategories.AddAsync(category, cancellationToken);
    }

    public Task UpdateAsync(LeaveCategory category, CancellationToken cancellationToken = default)
    {
        _dbContext.LeaveCategories.Update(category);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
