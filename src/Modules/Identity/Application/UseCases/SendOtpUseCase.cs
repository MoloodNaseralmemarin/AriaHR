using AriaHR.Modules.Identity.Application.Common;
using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Options;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Application.Services;
using AriaHR.Modules.Identity.Domain.Entities;
using Microsoft.Extensions.Options;

namespace AriaHR.Modules.Identity.Application.UseCases;

public record SendOtpResult(
    bool Success,
    string? ErrorMessage = null,
    string? OtpCode = null);

public class SendOtpUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpCodeRepository _otpCodeRepository;
    private readonly IAuthNotificationService _notificationService;
    private readonly ITokenService _tokenService;
    private readonly OtpOptions _otpOptions;

    public SendOtpUseCase(
        IUserRepository userRepository,
        IOtpCodeRepository otpCodeRepository,
        IAuthNotificationService notificationService,
        ITokenService tokenService,
        IOptions<OtpOptions> otpOptions)
    {
        _userRepository = userRepository;
        _otpCodeRepository = otpCodeRepository;
        _notificationService = notificationService;
        _tokenService = tokenService;
        _otpOptions = otpOptions.Value;
    }

    public async Task<SendOtpResult> ExecuteAsync(SendOtpRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return new SendOtpResult(false, "شماره تلفن الزامی است.");
        }

        string normalizedPhone = MobileNumberNormalizer.Normalize(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return new SendOtpResult(false, "فرمت شماره تلفن نامعتبر است.");
        }

        var user = await _userRepository.GetByPhoneNumberAsync(normalizedPhone, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return new SendOtpResult(false, "کاربری با این شماره تلفن یافت نشد یا حساب غیرفعال است.");
        }

        await _otpCodeRepository.InvalidateActiveCodesAsync(user.Id, cancellationToken);

        int minVal = (int)Math.Pow(10, Math.Max(1, _otpOptions.CodeLength - 1));
        int maxVal = (int)Math.Pow(10, _otpOptions.CodeLength) - 1;
        string rawCode = Random.Shared.Next(minVal, maxVal + 1).ToString().PadLeft(_otpOptions.CodeLength, '0');

        string codeHash = _tokenService.HashToken(rawCode);

        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PhoneNumber = normalizedPhone,
            CodeHash = codeHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_otpOptions.ExpirationMinutes),
            IsUsed = false,
            AttemptCount = 0
        };

        await _otpCodeRepository.AddAsync(otpCode, cancellationToken);
        await _notificationService.SendOtpAsync(normalizedPhone, rawCode, cancellationToken);

        return new SendOtpResult(true, null, rawCode);
    }
}
