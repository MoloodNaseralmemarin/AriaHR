using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Application.Services;

namespace AriaHR.Modules.Identity.Application.UseCases.ForgotPassword;

public class ResetPasswordUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetRepository _passwordResetRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public ResetPasswordUseCase(
        IUserRepository userRepository,
        IPasswordResetRepository passwordResetRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordResetRepository = passwordResetRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<bool> ExecuteAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) ||
            string.IsNullOrWhiteSpace(request.VerificationCode) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return false;
        }

        var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        string codeHash = _tokenService.HashToken(request.VerificationCode);
        var challenge = await _passwordResetRepository.GetLatestValidChallengeAsync(user.Id, codeHash, cancellationToken);

        if (challenge == null || challenge.IsUsed || challenge.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return false;
        }

        var now = DateTime.UtcNow;

        user.PasswordHash = _passwordService.HashPassword(user, request.NewPassword);
        user.UpdatedAtUtc = now;
        await _userRepository.UpdateAsync(user, cancellationToken);

        challenge.IsUsed = true;
        challenge.UsedAtUtc = now;
        challenge.UpdatedAtUtc = now;
        await _passwordResetRepository.UpdateChallengeAsync(challenge, cancellationToken);

        // Revoke all existing authentication sessions for user
        await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, cancellationToken);

        return true;
    }
}
