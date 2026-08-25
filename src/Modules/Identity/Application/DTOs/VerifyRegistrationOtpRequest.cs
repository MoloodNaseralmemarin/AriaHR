namespace AriaHR.Modules.Identity.Application.DTOs;

public record VerifyRegistrationOtpRequest(string MobileNumber, string Code);
