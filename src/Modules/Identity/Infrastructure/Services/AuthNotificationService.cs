using AriaHR.Modules.Identity.Application.Services;
using Microsoft.Extensions.Logging;

namespace AriaHR.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation for authentication notification delivery.
/// </summary>
public class AuthNotificationService : IAuthNotificationService
{
    private readonly ILogger<AuthNotificationService> _logger;

    public AuthNotificationService(ILogger<AuthNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NOTIFICATION] Password reset code generated for phone {PhoneNumber}.", phoneNumber);
        return Task.CompletedTask;
    }

    public Task SendRegistrationOtpAsync(string mobileNumber, string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NOTIFICATION] Registration OTP code generated for mobile {MobileNumber}.", mobileNumber);
        return Task.CompletedTask;
    }

    public Task SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NOTIFICATION] Login OTP code generated for phone {PhoneNumber}: {Code}", phoneNumber, code);
        return Task.CompletedTask;
    }
}
