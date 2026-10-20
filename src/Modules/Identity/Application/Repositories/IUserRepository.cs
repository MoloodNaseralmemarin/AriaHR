using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<int> GetCountByRoleNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CenterManagerDto>> GetCenterManagersAsync(Guid? organizationId = null, CancellationToken cancellationToken = default);
}
