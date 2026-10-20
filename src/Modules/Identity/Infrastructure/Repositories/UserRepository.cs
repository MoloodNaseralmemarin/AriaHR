using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == username || u.Email == username, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetCountByRoleNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await (from user in _dbContext.Users.AsNoTracking()
                      join userRole in _dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                      join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                      where !user.IsDeleted && user.IsActive && role.Name == roleName
                      select user.Id)
                     .Distinct()
                     .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CenterManagerDto>> GetCenterManagersAsync(Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        var query = from user in _dbContext.Users.AsNoTracking()
                    join userRole in _dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                    join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                    where !user.IsDeleted && role.Name == "CenterManager"
                    select user;

        if (organizationId.HasValue)
        {
            query = query.Where(u => u.OrganizationId == organizationId.Value);
        }

        return await query
            .Distinct()
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new CenterManagerDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                Email = u.Email,
                Gender = u.Gender,
                OrganizationId = u.OrganizationId,
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
