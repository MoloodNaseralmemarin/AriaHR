using AriaHR.Modules.Identity.Application.Common;
using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Options;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Application.Services;
using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.UseCases.Registration;

public class VerifyRegistrationOtpUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPendingRegistrationRepository _pendingRegistrationRepository;
    private readonly ITokenService _tokenService;
    private readonly OtpOptions _otpOptions;

    public VerifyRegistrationOtpUseCase(
        IUserRepository userRepository,
        IPendingRegistrationRepository pendingRegistrationRepository,
        ITokenService tokenService,
        OtpOptions? otpOptions = null)
    {
        _userRepository = userRepository;
        _pendingRegistrationRepository = pendingRegistrationRepository;
        _tokenService = tokenService;
        _otpOptions = otpOptions ?? new OtpOptions();
    }

    public async Task<VerifyRegistrationOtpResponse> ExecuteAsync(
        VerifyRegistrationOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber) || string.IsNullOrWhiteSpace(request.Code))
        {
            return new VerifyRegistrationOtpResponse(false, "invalid-input", "Mobile number and verification code are required.");
        }

        string normalizedMobile = MobileNumberNormalizer.Normalize(request.MobileNumber);
        var pendingRegistration = await _pendingRegistrationRepository.GetByMobileNumberAsync(normalizedMobile, cancellationToken);

        if (pendingRegistration == null)
        {
            return new VerifyRegistrationOtpResponse(false, "invalid-request", "No active registration request found for this mobile number.");
        }

        var now = DateTime.UtcNow;

        if (pendingRegistration.HasReachedMaxAttempts(_otpOptions.MaxAttempts))
        {
            return new VerifyRegistrationOtpResponse(false, "max-attempts-exceeded", "Maximum verification attempts reached.");
        }

        if (pendingRegistration.IsExpired(now))
        {
            return new VerifyRegistrationOtpResponse(false, "otp-expired", "Verification code has expired.");
        }

        string inputHash = _tokenService.HashToken(request.Code);

        if (!string.Equals(inputHash, pendingRegistration.VerificationCodeHash, StringComparison.OrdinalIgnoreCase))
        {
            pendingRegistration.IncrementAttemptCount();
            await _pendingRegistrationRepository.UpdateAsync(pendingRegistration, cancellationToken);
            return new VerifyRegistrationOtpResponse(false, "invalid-otp", "Invalid verification code.");
        }

        // OTP is correct - Create User
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = normalizedMobile,
            PhoneNumber = normalizedMobile,
            Email = string.Empty,
            PasswordHash = string.Empty,
            IsActive = true,
            CreatedAtUtc = now
        };

        await _userRepository.AddAsync(user, cancellationToken);

        // Delete PendingRegistration
        await _pendingRegistrationRepository.DeleteAsync(pendingRegistration, cancellationToken);

        return new VerifyRegistrationOtpResponse(true, "login");
    }
}
