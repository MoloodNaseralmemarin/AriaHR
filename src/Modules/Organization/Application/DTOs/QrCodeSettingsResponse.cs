namespace AriaHR.Modules.Organization.Application.DTOs;

public class QrCodeSettingsResponse
{
    public Guid WorkLocationId { get; set; }
    public Guid OrganizationId { get; set; }
    public bool VerificationEnabled { get; set; }
    public string Mode { get; set; } = string.Empty;
    public int RotationMinutes { get; set; }
    public IReadOnlyList<int> PredefinedRotationMinutes { get; set; } = Array.Empty<int>();
    public int MinRotationMinutes { get; set; }
    public int MaxRotationMinutes { get; set; }
    public string? CurrentCode { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int? RemainingValiditySeconds { get; set; }
}
