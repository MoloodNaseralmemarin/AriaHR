using AriaHR.Shared;

namespace AriaHR.Modules.Identity.Domain.Entities;

/// <summary>
/// PendingRegistration entity representing a temporary registration workflow state.
/// A User is only created after successful OTP verification.
/// </summary>
public class PendingRegistration : BaseEntity
{
    public string MobileNumber { get; set; } = string.Empty;
    public string VerificationCodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public bool IsVerified { get; set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    public bool HasReachedMaxAttempts(int maxAttempts) => AttemptCount >= maxAttempts;

    public void IncrementAttemptCount()
    {
        AttemptCount++;
    }

    public void UpdateOtp(string newCodeHash, DateTime newExpiresAtUtc)
    {
        VerificationCodeHash = newCodeHash;
        ExpiresAtUtc = newExpiresAtUtc;
        AttemptCount = 0;
        IsVerified = false;
    }
}
