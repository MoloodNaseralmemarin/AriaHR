namespace AriaHR.Modules.Identity.Application.DTOs;

public record VerifyOtpResponse(
    string AccessToken,
    DateTime ExpiresAt,
    UserResponse User);
