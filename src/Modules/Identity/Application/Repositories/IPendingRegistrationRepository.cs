using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.Repositories;

public interface IPendingRegistrationRepository
{
    Task<PendingRegistration?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default);
    Task AddAsync(PendingRegistration pendingRegistration, CancellationToken cancellationToken = default);
    Task UpdateAsync(PendingRegistration pendingRegistration, CancellationToken cancellationToken = default);
    Task DeleteAsync(PendingRegistration pendingRegistration, CancellationToken cancellationToken = default);
}
