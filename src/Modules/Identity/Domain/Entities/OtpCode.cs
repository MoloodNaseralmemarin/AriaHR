using AriaHR.Shared;

namespace AriaHR.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a one-time password (OTP) code challenge generated for authentication.
/// </summary>
public class OtpCode : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }
    public int AttemptCount { get; set; }
}
