using AriaHR.Modules.Organization.Domain.Enums;

namespace AriaHR.Modules.Organization.Application.DTOs;

/// <summary>
/// Represents a signed QR token for a work location.
/// For a dynamic QR the token is only valid until <see cref="ExpiresAtUtc"/>; the display client should
/// request a fresh token at <see cref="RefreshAfterUtc"/>.
/// </summary>
public class QrCodeResponse
{
    public Guid WorkLocationId { get; set; }
    public string QrCodeMode { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? RefreshAfterUtc { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public bool QrCodeEnabled { get; set; }
    public int RotationIntervalMinutes { get; set; }
}
