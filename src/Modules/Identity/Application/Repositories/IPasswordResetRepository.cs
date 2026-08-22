using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.Repositories;

public interface IPasswordResetRepository
{
    Task AddChallengeAsync(PasswordResetChallenge challenge, CancellationToken cancellationToken = default);
    Task<PasswordResetChallenge?> GetLatestValidChallengeAsync(Guid userId, string codeHash, CancellationToken cancellationToken = default);
    Task UpdateChallengeAsync(PasswordResetChallenge challenge, CancellationToken cancellationToken = default);
}
