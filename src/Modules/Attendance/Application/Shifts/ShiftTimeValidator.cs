using AriaHR.Modules.Attendance.Application.Exceptions;
using AriaHR.Modules.Attendance.Application.Options;
using AriaHR.Shared.Services;

namespace AriaHR.Modules.Attendance.Application.Shifts;

public record ShiftTimeEvaluation(bool IsLate, bool IsEarlyDeparture);

/// <summary>
/// Evaluates check-in / check-out times against an employee's assigned shift.
/// Overnight shifts (end time earlier than start time) are supported via circular minute arithmetic.
/// </summary>
public static class ShiftTimeValidator
{
    public static ShiftTimeEvaluation ValidateCheckIn(
        EmployeeShiftAssignmentDto shift,
        TimeOnly localTime,
        AttendancePolicyOptions options)
    {
        var (deltaMinutes, durationMinutes) = Evaluate(shift, localTime);

        bool withinWindow =
            deltaMinutes >= -options.EarlyCheckInWindowMinutes &&
            deltaMinutes <= durationMinutes + options.LateCheckInToleranceMinutes;

        if (!withinWindow)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.ShiftValidationFailed,
                $"ثبت ورود خارج از بازه مجاز شیفت «{shift.ShiftName}» امکان‌پذیر نیست.",
                400);
        }

        bool isLate = deltaMinutes > options.LateGraceMinutes;
        return new ShiftTimeEvaluation(isLate, IsEarlyDeparture: false);
    }

    public static ShiftTimeEvaluation ValidateCheckOut(
        EmployeeShiftAssignmentDto shift,
        TimeOnly localTime,
        AttendancePolicyOptions options)
    {
        var (deltaMinutes, durationMinutes) = Evaluate(shift, localTime);

        if (deltaMinutes < -options.EarlyCheckInWindowMinutes)
        {
            throw new AttendanceScanException(
                AttendanceErrorCodes.ShiftValidationFailed,
                $"ثبت خروج پیش از شروع بازه مجاز شیفت «{shift.ShiftName}» امکان‌پذیر نیست.",
                400);
        }

        bool isEarly = deltaMinutes < durationMinutes - options.EarlyDepartureToleranceMinutes;
        return new ShiftTimeEvaluation(IsLate: false, IsEarlyDeparture: isEarly);
    }

    private static (int DeltaMinutes, int DurationMinutes) Evaluate(EmployeeShiftAssignmentDto shift, TimeOnly localTime)
    {
        int startMinutes = shift.StartTime.Hour * 60 + shift.StartTime.Minute;
        int endMinutes = shift.EndTime.Hour * 60 + shift.EndTime.Minute;
        int nowMinutes = localTime.Hour * 60 + localTime.Minute;

        int durationMinutes = endMinutes - startMinutes;
        if (durationMinutes <= 0)
        {
            durationMinutes += 24 * 60;
        }

        int deltaMinutes = nowMinutes - startMinutes;
        if (deltaMinutes < -720)
        {
            deltaMinutes += 24 * 60;
        }
        else if (deltaMinutes > 720)
        {
            deltaMinutes -= 24 * 60;
        }

        return (deltaMinutes, durationMinutes);
    }
}
