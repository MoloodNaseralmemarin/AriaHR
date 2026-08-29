using AriaHR.Modules.Identity.Application.Repositories;

namespace AriaHR.Modules.Identity.Application.UseCases;

public class LogoutUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutUseCase(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
    }

    public async Task ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, cancellationToken);
    }
}
