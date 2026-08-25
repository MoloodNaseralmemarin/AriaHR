namespace AriaHR.Modules.Identity.Application.DTOs;

public record LoginRequest(string NationalCode, string Password)
{
    public string Username => NationalCode;
}
