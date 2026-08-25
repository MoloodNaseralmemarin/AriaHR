namespace AriaHR.Modules.Identity.Application.Options;

public class OtpOptions
{
    public const string SectionName = "Otp";

    public int CodeLength { get; set; } = 4;
    public int ExpirationMinutes { get; set; } = 2;
    public int MaxAttempts { get; set; } = 5;
}
