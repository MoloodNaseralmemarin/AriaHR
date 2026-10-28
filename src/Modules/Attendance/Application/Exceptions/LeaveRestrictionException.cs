namespace AriaHR.Modules.Attendance.Application.Exceptions;

public class LeaveRestrictionException : InvalidOperationException
{
    public const string DefaultErrorCode = "ATTENDANCE_BLOCKED_BY_APPROVED_LEAVE";
    public const string DefaultMessage = "امکان ثبت تردد در بازه مرخصی تأییدشده وجود ندارد.";

    public string ErrorCode { get; }

    public LeaveRestrictionException(string message = DefaultMessage, string errorCode = DefaultErrorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
