using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Identity.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _dbContext;

    public RoleRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _dbContext.Roles.AddAsync(role, cancellationToken);
    }

    public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _dbContext.Roles.Update(role);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Role role, CancellationToken cancellationToken = default)
    {
        _dbContext.Roles.Remove(role);
        return Task.CompletedTask;
    }
}
