namespace AriaHR.Modules.Attendance.Application.Options;

public class AttendancePolicyOptions
{
    public const string SectionName = "Attendance:Policy";

    /// <summary>
    /// When true, an employee with an active shift assignment must scan inside the shift window.
    /// Employees without any assignment on the day are not blocked.
    /// </summary>
    public bool ShiftValidationEnabled { get; set; } = true;

    /// <summary>
    /// How early before the shift start a check-in is accepted.
    /// </summary>
    public int EarlyCheckInWindowMinutes { get; set; } = 120;

    /// <summary>
    /// How long after the shift end a check-in is still accepted.
    /// </summary>
    public int LateCheckInToleranceMinutes { get; set; } = 120;

    /// <summary>
    /// Grace period after the shift start before the attendance is flagged as late.
    /// </summary>
    public int LateGraceMinutes { get; set; } = 15;

    /// <summary>
    /// Tolerance before the shift end used to flag an early departure.
    /// </summary>
    public int EarlyDepartureToleranceMinutes { get; set; } = 15;
}
