using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Application.Services;
using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.UseCases.ForgotPassword;

public class ForgotPasswordUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetRepository _passwordResetRepository;
    private readonly IAuthNotificationService _notificationService;
    private readonly ITokenService _tokenService;

    public ForgotPasswordUseCase(
        IUserRepository userRepository,
        IPasswordResetRepository passwordResetRepository,
        IAuthNotificationService notificationService,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordResetRepository = passwordResetRepository;
        _notificationService = notificationService;
        _tokenService = tokenService;
    }

    public async Task ExecuteAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return;
        }

        var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        if (user == null || !user.IsActive)
        {
            // Do not expose user existence
            return;
        }

        // Generate 6-digit numeric verification code
        Random random = new Random();
        string verificationCode = random.Next(100000, 999999).ToString();
        string codeHash = _tokenService.HashToken(verificationCode);

        var now = DateTime.UtcNow;
        var challenge = new PasswordResetChallenge
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PhoneNumber = request.PhoneNumber,
            CodeHash = codeHash,
            ExpiresAtUtc = now.AddMinutes(15),
            IsUsed = false,
            CreatedAtUtc = now
        };

        await _passwordResetRepository.AddChallengeAsync(challenge, cancellationToken);

        await _notificationService.SendPasswordResetCodeAsync(
            request.PhoneNumber,
            verificationCode,
            cancellationToken);
    }
}
