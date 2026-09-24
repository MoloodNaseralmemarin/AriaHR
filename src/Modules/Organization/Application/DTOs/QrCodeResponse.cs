namespace AriaHR.Modules.Organization.Application.DTOs;

public class QrCodeResponse
{
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
