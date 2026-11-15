using AriaHR.Modules.Organization.Domain.Enums;

namespace AriaHR.Modules.Organization.Application.DTOs;

public class UpdateQrCodeSettingsRequest
{
    public bool VerificationEnabled { get; set; } = true;
    public QrCodeMode Mode { get; set; } = QrCodeMode.Dynamic;
    public int? RotationMinutes { get; set; }
}
