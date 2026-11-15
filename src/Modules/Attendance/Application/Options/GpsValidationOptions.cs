namespace AriaHR.Modules.Attendance.Application.Options;

public class GpsValidationOptions
{
    public const string SectionName = "Attendance:Gps";

    /// <summary>
    /// Maximum acceptable GPS accuracy (metres). Readings worse than this are rejected with
    /// LOCATION_ACCURACY_INSUFFICIENT so the employee can retry.
    /// </summary>
    public double MaxAccuracyMeters { get; set; } = 50;

    /// <summary>
    /// Maximum accepted age of a location reading, measured against server UTC time.
    /// </summary>
    public int MaxLocationAgeSeconds { get; set; } = 120;

    /// <summary>
    /// Allowed clock skew for a reading whose timestamp is slightly in the future.
    /// </summary>
    public int MaxFutureSkewSeconds { get; set; } = 30;
}
