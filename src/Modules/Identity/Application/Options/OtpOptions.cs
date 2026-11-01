namespace AriaHR.Modules.Identity.Application.Options;

public class OtpOptions
{
    public const string SectionName = "Otp";

    /// <summary>
    /// Temporary development/testing setting: When true, static fixed OTP is used and SMS delivery is bypassed.
    /// Set to false when switching to real SMS-based verification in production.
    /// </summary>
    public bool UseStaticCode { get; set; } = true;

    /// <summary>
    /// Temporary fixed OTP value used when <see cref="UseStaticCode"/> is true (e.g., "1234").
    /// </summary>
    public string StaticCode { get; set; } = "1234";

    public int CodeLength { get; set; } = 4;
    public int ExpirationMinutes { get; set; } = 2;
    public int MaxAttempts { get; set; } = 5;
}
