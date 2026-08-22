using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Application.Services;
using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.UseCases.Login;

public class LoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public LoginUseCase(
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<AuthenticationResponse?> ExecuteAsync(
        LoginRequest request,
        string? clientIp = null,
        CancellationToken cancellationToken = default)
    {
        var nationalCode = !string.IsNullOrWhiteSpace(request.NationalCode) ? request.NationalCode : request.Username;
        if (string.IsNullOrWhiteSpace(nationalCode) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var user = await _userRepository.GetByUsernameAsync(nationalCode, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        bool isPasswordValid = _passwordService.VerifyPassword(user, user.PasswordHash, request.Password);
        if (!isPasswordValid)
        {
            return null;
        }

        var roles = await _userRoleRepository.GetRolesByUserIdAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();

        string accessToken = _tokenService.GenerateAccessToken(user, roleNames);
        string plainRefreshToken = _tokenService.GenerateRefreshToken();
        string refreshTokenHash = _tokenService.HashToken(plainRefreshToken);

        var now = DateTime.UtcNow;
        var accessExpiration = now.AddMinutes(_tokenService.AccessTokenExpirationMinutes);
        var refreshExpiration = now.AddDays(_tokenService.RefreshTokenExpirationDays);

        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAtUtc = refreshExpiration,
            IsRevoked = false,
            CreatedByIp = clientIp,
            CreatedAtUtc = now
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        user.LastLoginAt = now;
        user.UpdatedAtUtc = now;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var expiresIn = (int)Math.Max(0, (accessExpiration - now).TotalSeconds);

        return new AuthenticationResponse(
            accessToken,
            plainRefreshToken,
            accessExpiration,
            refreshExpiration,
            "Bearer",
            expiresIn);
    }
}
