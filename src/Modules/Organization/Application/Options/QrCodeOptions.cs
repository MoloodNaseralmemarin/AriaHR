namespace AriaHR.Modules.Organization.Application.Options;

public class QrCodeOptions
{
    public const string SectionName = "QrCode";

    public const int DefaultRotationIntervalMinutes = 5;
    public const int MinRotationMinutes = 1;
    public const int MaxRotationMinutes = 1440;

    public static readonly IReadOnlyList<int> PredefinedRotationMinutes = new[] { 1, 5, 10, 15, 30, 60 };

    /// <summary>
    /// Rotation interval (in minutes) used when a work location has no valid configured interval.
    /// </summary>
    public int DefaultRotationMinutes { get; set; } = DefaultRotationIntervalMinutes;
}
