using System.Security.Cryptography;
using AriaHR.Modules.Identity.Application.Common;
using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Options;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Application.Services;
using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.UseCases.Registration;

public class InitiateRegistrationUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPendingRegistrationRepository _pendingRegistrationRepository;
    private readonly IAuthNotificationService _notificationService;
    private readonly ITokenService _tokenService;
    private readonly OtpOptions _otpOptions;

    public InitiateRegistrationUseCase(
        IUserRepository userRepository,
        IPendingRegistrationRepository pendingRegistrationRepository,
        IAuthNotificationService notificationService,
        ITokenService tokenService,
        OtpOptions? otpOptions = null)
    {
        _userRepository = userRepository;
        _pendingRegistrationRepository = pendingRegistrationRepository;
        _notificationService = notificationService;
        _tokenService = tokenService;
        _otpOptions = otpOptions ?? new OtpOptions();
    }

    public async Task<InitiateRegistrationResponse> ExecuteAsync(
        InitiateRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber))
        {
            return new InitiateRegistrationResponse(false, "invalid-input");
        }

        string normalizedMobile = MobileNumberNormalizer.Normalize(request.MobileNumber);
        if (string.IsNullOrWhiteSpace(normalizedMobile))
        {
            return new InitiateRegistrationResponse(false, "invalid-input");
        }

        // Check if User already exists with this mobile number (checked via PhoneNumber or Username)
        var existingUser = await _userRepository.GetByPhoneNumberAsync(normalizedMobile, cancellationToken)
            ?? await _userRepository.GetByUsernameAsync(normalizedMobile, cancellationToken);

        if (existingUser != null)
        {
            // User exists: DO NOT create PendingRegistration or send OTP.
            return new InitiateRegistrationResponse(true, "login");
        }

        // Generate cryptographically secure 4-digit OTP
        int codeInt = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, _otpOptions.CodeLength));
        string code = codeInt.ToString($"D{_otpOptions.CodeLength}");
        string codeHash = _tokenService.HashToken(code);
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(_otpOptions.ExpirationMinutes);

        var pendingRegistration = await _pendingRegistrationRepository.GetByMobileNumberAsync(normalizedMobile, cancellationToken);

        if (pendingRegistration != null)
        {
            pendingRegistration.UpdateOtp(codeHash, expiresAtUtc);
            await _pendingRegistrationRepository.UpdateAsync(pendingRegistration, cancellationToken);
        }
        else
        {
            pendingRegistration = new PendingRegistration
            {
                Id = Guid.NewGuid(),
                MobileNumber = normalizedMobile,
                VerificationCodeHash = codeHash,
                ExpiresAtUtc = expiresAtUtc,
                CreatedAtUtc = DateTime.UtcNow,
                AttemptCount = 0,
                IsVerified = false
            };
            await _pendingRegistrationRepository.AddAsync(pendingRegistration, cancellationToken);
        }

        await _notificationService.SendRegistrationOtpAsync(normalizedMobile, code, cancellationToken);

        return new InitiateRegistrationResponse(true, "verify-otp");
    }
}
