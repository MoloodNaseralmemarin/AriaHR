namespace AriaHR.Modules.Organization.Application.Options;

public class QrCodeOptions
{
    public const string SectionName = "QrCode";

    /// <summary>
    /// Legacy sliding expiration (minutes) kept for the legacy DB-backed QR endpoint.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Optional dedicated HMAC key for QR tokens. When empty, the JWT secret is used as the key material.
    /// </summary>
    public string? SigningKey { get; set; }

    public int DefaultRotationIntervalMinutes { get; set; } = 5;
    public int MinRotationIntervalMinutes { get; set; } = 1;
    public int MaxRotationIntervalMinutes { get; set; } = 60;

    public decimal MinAllowedRadiusMeters { get; set; } = 1;
    public decimal MaxAllowedRadiusMeters { get; set; } = 1000;

    public int ClampRotationInterval(int? requested)
    {
        int min = MinRotationIntervalMinutes > 0 ? MinRotationIntervalMinutes : 1;
        int max = MaxRotationIntervalMinutes >= min ? MaxRotationIntervalMinutes : min;

        int value = requested.HasValue && requested.Value > 0
            ? requested.Value
            : (DefaultRotationIntervalMinutes > 0 ? DefaultRotationIntervalMinutes : min);

        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
