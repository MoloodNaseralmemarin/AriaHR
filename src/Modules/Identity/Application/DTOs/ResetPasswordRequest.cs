namespace AriaHR.Modules.Identity.Application.DTOs;

public record ResetPasswordRequest(string PhoneNumber, string VerificationCode, string NewPassword);
