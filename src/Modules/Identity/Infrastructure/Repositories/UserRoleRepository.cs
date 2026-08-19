using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Identity.Infrastructure.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRoleRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<UserRole?> GetByUserAndRoleIdAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Include(ur => ur.Role)
            .Include(ur => ur.User)
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetRolesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role!)
            .Where(r => r != null)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserRoles.AddAsync(userRole, cancellationToken);
    }

    public Task RemoveAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        _dbContext.UserRoles.Remove(userRole);
        return Task.CompletedTask;
    }

    public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
    }
}
