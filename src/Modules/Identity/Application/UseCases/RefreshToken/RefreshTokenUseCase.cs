using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Application.Services;
using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.UseCases.RefreshToken;

public class RefreshTokenUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ITokenService _tokenService;

    public RefreshTokenUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthenticationResponse?> ExecuteAsync(
        RefreshTokenRequest request,
        string? clientIp = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return null;
        }

        string incomingHash = _tokenService.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(incomingHash, cancellationToken);

        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        // Generate new refresh token
        string newPlainRefreshToken = _tokenService.GenerateRefreshToken();
        string newRefreshTokenHash = _tokenService.HashToken(newPlainRefreshToken);

        // Revoke old token
        storedToken.IsRevoked = true;
        storedToken.RevokedAtUtc = now;
        storedToken.ReplacedByTokenHash = newRefreshTokenHash;
        storedToken.UpdatedAtUtc = now;
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

        // Add new token
        var newAccessExpiration = now.AddMinutes(_tokenService.AccessTokenExpirationMinutes);
        var newRefreshExpiration = now.AddDays(_tokenService.RefreshTokenExpirationDays);

        var newRefreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAtUtc = newRefreshExpiration,
            IsRevoked = false,
            CreatedByIp = clientIp,
            CreatedAtUtc = now
        };

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        // Load roles and issue new JWT
        var roles = await _userRoleRepository.GetRolesByUserIdAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();

        string newAccessToken = _tokenService.GenerateAccessToken(user, roleNames);

        return new AuthenticationResponse(
            newAccessToken,
            newPlainRefreshToken,
            newAccessExpiration,
            newRefreshExpiration);
    }
}
