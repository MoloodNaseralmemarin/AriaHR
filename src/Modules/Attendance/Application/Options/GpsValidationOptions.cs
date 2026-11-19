namespace AriaHR.Modules.Attendance.Application.Options;

public class GpsValidationOptions
{
    public const string SectionName = "Attendance:Gps";

    /// <summary>
    /// Gate R1 — absolute accuracy admission limit (metres). A reading whose reported accuracy is worse
    /// than this is rejected with LOCATION_ACCURACY_INSUFFICIENT so the employee can retry.
    /// This value is independent of any work location's allowed radius and must never be compared with it.
    /// </summary>
    public double MaxGpsAccuracyInMeters { get; set; } = 50;

    /// <summary>
    /// Gate R2 — maximum uncertainty credit (metres) that may be subtracted from the measured distance
    /// when deciding geofence membership. A client-declared accuracy larger than this is capped here so
    /// spoofed readings cannot widen the fence without bound.
    /// </summary>
    public double MaxAssumedUncertaintyMeters { get; set; } = 20;

    /// <summary>
    /// Maximum accepted age of a location reading, measured against server UTC time.
    /// </summary>
    public int MaxLocationAgeSeconds { get; set; } = 120;

    /// <summary>
    /// Allowed clock skew for a reading whose timestamp is slightly in the future.
    /// </summary>
    public int MaxFutureSkewSeconds { get; set; } = 30;
}
