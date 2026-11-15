namespace AriaHR.Modules.Attendance.Application;

public static class AttendanceErrorCodes
{
    public const string AttendanceRegistered = "ATTENDANCE_REGISTERED";
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string QrCodeInvalid = "QR_CODE_INVALID";
    public const string QrCodeExpired = "QR_CODE_EXPIRED";
    public const string QrCodeDisabled = "QR_CODE_DISABLED";
    public const string WorkLocationNotFound = "WORK_LOCATION_NOT_FOUND";
    public const string WorkLocationInactive = "WORK_LOCATION_INACTIVE";
    public const string EmployeeNotFound = "EMPLOYEE_NOT_FOUND";
    public const string EmployeeNotAuthorized = "EMPLOYEE_NOT_AUTHORIZED";
    public const string LocationUnavailable = "LOCATION_UNAVAILABLE";
    public const string LocationCoordinatesInvalid = "LOCATION_COORDINATES_INVALID";
    public const string LocationAccuracyInsufficient = "LOCATION_ACCURACY_INSUFFICIENT";
    public const string LocationDataExpired = "LOCATION_DATA_EXPIRED";
    public const string LocationTimestampInvalid = "LOCATION_TIMESTAMP_INVALID";
    public const string OutsideAllowedRadius = "OUTSIDE_ALLOWED_RADIUS";
    public const string ShiftValidationFailed = "SHIFT_VALIDATION_FAILED";
    public const string DuplicateAttendance = "DUPLICATE_ATTENDANCE";
    public const string InvalidAttendanceAction = "INVALID_ATTENDANCE_ACTION";
    public const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
}
