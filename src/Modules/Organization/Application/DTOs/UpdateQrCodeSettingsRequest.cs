using AriaHR.Modules.Organization.Domain.Enums;

namespace AriaHR.Modules.Organization.Application.DTOs;

public class UpdateQrCodeSettingsRequest
{
    public QrCodeMode QrCodeMode { get; set; } = QrCodeMode.Static;
    public int? QrRotationIntervalMinutes { get; set; }
}
