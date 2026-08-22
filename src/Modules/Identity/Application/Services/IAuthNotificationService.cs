namespace AriaHR.Modules.Identity.Application.Services;

public interface IAuthNotificationService
{
    Task SendPasswordResetCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}
