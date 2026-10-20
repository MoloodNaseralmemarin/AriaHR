using System.Globalization;

namespace AriaHR.Shared.Utils;

public static class JalaliCalendarHelper
{
    private static readonly PersianCalendar Pc = new();

    /// <summary>
    /// Gets the start of the current Jalali month in UTC and the start of the next Jalali month in UTC.
    /// Uses a half-open interval [StartOfJalaliMonthUtc, StartOfNextJalaliMonthUtc).
    /// </summary>
    public static (DateTime StartOfJalaliMonthUtc, DateTime StartOfNextJalaliMonthUtc) GetCurrentJalaliMonthBoundariesUtc(DateTime? referenceDateUtc = null)
    {
        var nowUtc = referenceDateUtc ?? DateTime.UtcNow;

        int year = Pc.GetYear(nowUtc);
        int month = Pc.GetMonth(nowUtc);

        var startOfJalaliMonthUtc = DateTime.SpecifyKind(Pc.ToDateTime(year, month, 1, 0, 0, 0, 0), DateTimeKind.Utc);
        var startOfNextJalaliMonthUtc = DateTime.SpecifyKind(Pc.AddMonths(startOfJalaliMonthUtc, 1), DateTimeKind.Utc);

        return (startOfJalaliMonthUtc, startOfNextJalaliMonthUtc);
    }
}
