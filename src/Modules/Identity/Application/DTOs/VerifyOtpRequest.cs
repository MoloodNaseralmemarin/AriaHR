namespace AriaHR.Modules.Identity.Application.DTOs;

public record VerifyOtpRequest(string PhoneNumber, string Code);
