using AriaHR.Modules.Organization.Domain.Enums;
using AriaHR.Shared;

namespace AriaHR.Modules.Organization.Domain.Entities;

/// <summary>
/// WorkLocation entity representing a physical work site with geofencing and QR code configuration.
/// </summary>
public class WorkLocation : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal AllowedRadiusInMeters { get; set; }

    public bool IsActive { get; set; }

    public QrCodeMode QrCodeMode { get; set; } = QrCodeMode.Static;
    public string QrCodeIdentifier { get; set; } = string.Empty;
    public bool QrCodeEnabled { get; set; }
    public int QrRotationIntervalMinutes { get; set; } = 5;
    public DateTime? QrCodeLastGeneratedAtUtc { get; set; }

    public ICollection<QRCode> QRCodes { get; set; } = new List<QRCode>();
}
