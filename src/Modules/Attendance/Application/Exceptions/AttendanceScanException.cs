using AriaHR.Modules.Attendance.Application.DTOs;

namespace AriaHR.Modules.Attendance.Application.Exceptions;

/// <summary>
/// Carries a stable error code, HTTP status and optional diagnostic data for attendance scan rejections.
/// </summary>
public class AttendanceScanException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public AttendanceScanData? Data { get; }

    public AttendanceScanException(string code, string message, int statusCode = 400, AttendanceScanData? data = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Data = data;
    }
}
