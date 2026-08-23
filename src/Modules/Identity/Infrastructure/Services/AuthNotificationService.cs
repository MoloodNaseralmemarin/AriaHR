using AriaHR.Modules.Identity.Application.Services;
using Microsoft.Extensions.Logging;

namespace AriaHR.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Infrastructure placeholder implementation for authentication notification delivery.
/// Actual provider integration (e.g. SMS/Notification module event) is pending.
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
        _logger.LogInformation("[NOTIFICATION PENDING] Verification challenge code generated for phone {PhoneNumber}.", phoneNumber);
        return Task.CompletedTask;
    }

    public Task SendRegistrationOtpAsync(string mobileNumber, string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NOTIFICATION PENDING] Registration OTP code generated for mobile {MobileNumber}.", mobileNumber);
        return Task.CompletedTask;
    }
}
