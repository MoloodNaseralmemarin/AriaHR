namespace AriaHR.Shared.Utils;

public static class TimezoneHelper
{
    private static readonly TimeZoneInfo IranTimeZone = GetIranTimeZone();

    private static TimeZoneInfo GetIranTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("Iran Standard Time Fallback", TimeSpan.FromHours(3.5), "Iran Standard Time", "Iran Standard Time");
            }
        }
    }

    public static DateTime ConvertUtcToIranTime(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utc, IranTimeZone);
    }

    public static (DateOnly LocalDate, TimeOnly LocalTime) GetIranLocalComponents(DateTime utcDateTime)
    {
        var localDateTime = ConvertUtcToIranTime(utcDateTime);
        return (DateOnly.FromDateTime(localDateTime), TimeOnly.FromDateTime(localDateTime));
    }

    public static DateOnly GetIranToday(DateTime? referenceUtc = null)
    {
        var utc = referenceUtc ?? DateTime.UtcNow;
        return GetIranLocalComponents(utc).LocalDate;
    }
}
