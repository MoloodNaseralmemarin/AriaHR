using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.Repositories;

public interface IUserRoleRepository
{
    Task<UserRole?> GetByUserAndRoleIdAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetRolesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task RemoveAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}
