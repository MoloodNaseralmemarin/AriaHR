using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Identity.Infrastructure.Repositories;

public class PendingRegistrationRepository : IPendingRegistrationRepository
{
    private readonly IdentityDbContext _dbContext;

    public PendingRegistrationRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<PendingRegistration?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PendingRegistrations
            .FirstOrDefaultAsync(p => p.MobileNumber == mobileNumber, cancellationToken);
    }

    public async Task AddAsync(PendingRegistration pendingRegistration, CancellationToken cancellationToken = default)
    {
        await _dbContext.PendingRegistrations.AddAsync(pendingRegistration, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PendingRegistration pendingRegistration, CancellationToken cancellationToken = default)
    {
        _dbContext.PendingRegistrations.Update(pendingRegistration);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PendingRegistration pendingRegistration, CancellationToken cancellationToken = default)
    {
        _dbContext.PendingRegistrations.Remove(pendingRegistration);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
