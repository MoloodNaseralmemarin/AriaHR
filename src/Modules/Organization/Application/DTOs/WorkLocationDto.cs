using AriaHR.Modules.Organization.Domain.Enums;

namespace AriaHR.Modules.Organization.Application.DTOs;

public class WorkLocationDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal AllowedRadiusInMeters { get; set; }
    public bool IsActive { get; set; }

    public string QrCodeMode { get; set; } = string.Empty;
    public bool QrCodeEnabled { get; set; }
    public int QrRotationIntervalMinutes { get; set; }
    public DateTime? QrCodeLastGeneratedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
