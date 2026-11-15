namespace AriaHR.Modules.Organization.Application.DTOs;

public class QrCodeSettingsDto
{
    public Guid WorkLocationId { get; set; }
    public string QrCodeMode { get; set; } = string.Empty;
    public bool QrCodeEnabled { get; set; }
    public int RotationIntervalMinutes { get; set; }
    public int MinRotationIntervalMinutes { get; set; }
    public int MaxRotationIntervalMinutes { get; set; }
    public int DefaultRotationIntervalMinutes { get; set; }
    public DateTime? LastGeneratedAtUtc { get; set; }
    public bool IsDynamic { get; set; }
}
