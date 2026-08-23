namespace AriaHR.Modules.Identity.Application.DTOs;

public record VerifyRegistrationOtpResponse(bool Success, string NextStep, string? ErrorMessage = null);
