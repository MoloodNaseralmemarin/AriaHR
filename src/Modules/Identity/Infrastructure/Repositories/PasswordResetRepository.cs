using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Identity.Infrastructure.Repositories;

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly IdentityDbContext _dbContext;

    public PasswordResetRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddChallengeAsync(PasswordResetChallenge challenge, CancellationToken cancellationToken = default)
    {
        await _dbContext.PasswordResetChallenges.AddAsync(challenge, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PasswordResetChallenge?> GetLatestValidChallengeAsync(Guid userId, string codeHash, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PasswordResetChallenges
            .AsNoTracking()
            .Where(prc => prc.UserId == userId && prc.CodeHash == codeHash && !prc.IsUsed && prc.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(prc => prc.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateChallengeAsync(PasswordResetChallenge challenge, CancellationToken cancellationToken = default)
    {
        var local = _dbContext.PasswordResetChallenges.Local.FirstOrDefault(prc => prc.Id == challenge.Id);
        if (local != null)
        {
            _dbContext.Entry(local).State = EntityState.Detached;
        }

        _dbContext.PasswordResetChallenges.Update(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
