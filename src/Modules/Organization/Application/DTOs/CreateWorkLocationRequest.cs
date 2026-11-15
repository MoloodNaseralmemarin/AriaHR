using AriaHR.Modules.Organization.Domain.Enums;

namespace AriaHR.Modules.Organization.Application.DTOs;

public class CreateWorkLocationRequest
{
    public Guid? OrganizationId { get; set; }
    public string? Name { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal AllowedRadiusInMeters { get; set; }
    public bool IsActive { get; set; } = true;

    public QrCodeMode QrCodeMode { get; set; } = QrCodeMode.Static;
    public bool QrCodeEnabled { get; set; } = true;
    public int? QrRotationIntervalMinutes { get; set; }
}
