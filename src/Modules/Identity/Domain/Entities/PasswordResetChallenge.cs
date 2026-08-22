using AriaHR.Shared;

namespace AriaHR.Modules.Identity.Domain.Entities;

/// <summary>
/// PasswordResetChallenge entity representing password reset verification codes.
/// </summary>
public class PasswordResetChallenge : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}
