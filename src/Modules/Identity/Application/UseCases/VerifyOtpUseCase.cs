using AriaHR.Modules.Identity.Application.Common;
using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.Options;
using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Identity.Application.Services;
using Microsoft.Extensions.Options;

namespace AriaHR.Modules.Identity.Application.UseCases;

public record VerifyOtpResult(
    bool Success,
    string? ErrorMessage = null,
    VerifyOtpResponse? Response = null,
    string? ErrorType = null);

public class VerifyOtpUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpCodeRepository _otpCodeRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ITokenService _tokenService;
    private readonly OtpOptions _otpOptions;

    public VerifyOtpUseCase(
        IUserRepository userRepository,
        IOtpCodeRepository otpCodeRepository,
        IUserRoleRepository userRoleRepository,
        ITokenService tokenService,
        IOptions<OtpOptions> otpOptions)
    {
        _userRepository = userRepository;
        _otpCodeRepository = otpCodeRepository;
        _userRoleRepository = userRoleRepository;
        _tokenService = tokenService;
        _otpOptions = otpOptions.Value;
    }

    public async Task<VerifyOtpResult> ExecuteAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Code))
        {
            return new VerifyOtpResult(false, "شماره تلفن و کد تایید الزامی است.");
        }

        string normalizedPhone = MobileNumberNormalizer.Normalize(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return new VerifyOtpResult(false, "فرمت شماره تلفن نامعتبر است.");
        }

        var user = await _userRepository.GetByPhoneNumberAsync(normalizedPhone, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return new VerifyOtpResult(false, "کاربری با این شماره تلفن یافت نشد یا حساب غیرفعال است.");
        }

        var otpCode = await _otpCodeRepository.GetLatestActiveCodeAsync(user.Id, normalizedPhone, cancellationToken);
        if (otpCode == null || otpCode.IsUsed)
        {
            return new VerifyOtpResult(false, "کد تایید نامعتبر یا استفاده شده است.", ErrorType: "INVALID_CODE");
        }

        if (otpCode.ExpiresAtUtc < DateTime.UtcNow)
        {
            return new VerifyOtpResult(false, "کد تایید منقضی شده است.", ErrorType: "EXPIRED_CODE");
        }

        if (otpCode.AttemptCount >= _otpOptions.MaxAttempts)
        {
            return new VerifyOtpResult(false, "تعداد تلاش‌های ناموفق بیش از حد مجاز است.", ErrorType: "MAX_ATTEMPTS_EXCEEDED");
        }

        string submittedHash = _tokenService.HashToken(request.Code);
        if (!string.Equals(submittedHash, otpCode.CodeHash, StringComparison.OrdinalIgnoreCase))
        {
            otpCode.AttemptCount++;
            await _otpCodeRepository.UpdateAsync(otpCode, cancellationToken);
            return new VerifyOtpResult(false, "کد تایید اشتباه است.", ErrorType: "INVALID_CODE");
        }

        otpCode.IsUsed = true;
        await _otpCodeRepository.UpdateAsync(otpCode, cancellationToken);

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var roles = await _userRoleRepository.GetRolesByUserIdAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();

        string accessToken = _tokenService.GenerateAccessToken(user, roleNames);
        var expiresAt = DateTime.UtcNow.AddMinutes(_tokenService.AccessTokenExpirationMinutes);

        var userDto = new UserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            roleNames,
            user.OrganizationId);

        var response = new VerifyOtpResponse(accessToken, expiresAt, userDto);

        return new VerifyOtpResult(true, Response: response);
    }
}
