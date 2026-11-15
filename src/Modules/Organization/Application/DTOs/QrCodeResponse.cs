namespace AriaHR.Modules.Organization.Application.DTOs;

public class QrCodeResponse
{
    public Guid? QrCodeId { get; set; }
    public Guid WorkLocationId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? Code { get; set; }
    public string Mode { get; set; } = string.Empty;
    public bool VerificationEnabled { get; set; }
    public DateTime? GeneratedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int? RemainingValiditySeconds { get; set; }
}
