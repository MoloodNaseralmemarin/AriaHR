namespace AriaHR.Modules.Identity.Application.Services;

public interface IAuthNotificationService
{
    Task SendPasswordResetCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
    Task SendRegistrationOtpAsync(string mobileNumber, string code, CancellationToken cancellationToken = default);
}
